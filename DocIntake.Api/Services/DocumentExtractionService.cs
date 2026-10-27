using System.Diagnostics;
using System.Text.Json;
using DocIntake.Api.Configuration;
using DocIntake.Api.Data;
using DocIntake.Api.Models;
using DocIntake.Api.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocIntake.Api.Services;

public enum ExtractionRunStatus { NotFound, AlreadyConfirmed, QuotaExceeded, Completed }

/// <summary>Outcome of one extraction run. When Completed, Extraction says whether it succeeded or failed.</summary>
public sealed record ExtractionRun(
    ExtractionRunStatus Status,
    Document? Document = null,
    Extraction? Extraction = null,
    IReadOnlyList<ValidationIssue>? Issues = null);

/// <summary>
/// The extraction pipeline: read text, classify, extract, validate, persist.
/// Every attempt, successful or not, is recorded as an Extraction row.
/// Logs identifiers, types, counts and timings only, never document content.
/// </summary>
public class DocumentExtractionService(
    AppDbContext db,
    ICurrentUser user,
    IOptions<QuotaOptions> quota,
    IDocumentStorage storage,
    IDocumentTextExtractor textExtractor,
    IDocumentExtractor extractor,
    ILogger<DocumentExtractionService> log)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ExtractionRun> RunAsync(int documentId, DocumentType? requestedType, CancellationToken ct)
    {
        var document = await db.Documents.OwnedBy(user.Id).FirstOrDefaultAsync(d => d.Id == documentId && !d.IsArchived, ct);
        if (document is null)
            return new ExtractionRun(ExtractionRunStatus.NotFound);
        if (document.Status == DocumentStatus.Confirmed)
        {
            log.LogWarning("Extraction refused: document {DocumentId} is already confirmed", documentId);
            return new ExtractionRun(ExtractionRunStatus.AlreadyConfirmed, document);
        }

        // Each run may call a paid model, so it counts against the user's daily cap before any work starts.
        if (!await TryChargeQuotaAsync(ct))
        {
            log.LogWarning("Extraction refused: daily limit of {Limit} reached", quota.Value.DailyExtractions);
            return new ExtractionRun(ExtractionRunStatus.QuotaExceeded, document);
        }

        DocumentType? type = requestedType is DocumentType.Unknown ? null : requestedType;

        // 1. Text: reuse what was read before, otherwise read the stored file.
        var text = await db.DocumentTexts.FirstOrDefaultAsync(t => t.DocumentId == document.Id, ct);
        if (text is null)
        {
            await using var stream = await storage.OpenReadAsync(document.StorageId, ct);
            if (stream is null)
                return await FailAsync(document, DocumentType.Unknown, "FileMissing", ct);

            ExtractedText extracted;
            try
            {
                extracted = await textExtractor.ExtractAsync(stream, document.FileType, ct);
            }
            catch (TextExtractionException)
            {
                return await FailAsync(document, DocumentType.Unknown, "TextExtractionFailed", ct);
            }

            if (string.IsNullOrWhiteSpace(extracted.Text))
                return await FailAsync(document, DocumentType.Unknown, "NoTextFound", ct);

            text = new DocumentText
            {
                DocumentId = document.Id,
                Text = extracted.Text,
                Method = extracted.Method,
                PageCount = extracted.PageCount,
                CharCount = extracted.Text.Length,
                Truncated = extracted.Truncated
            };
            db.DocumentTexts.Add(text);
            document.PageCount ??= extracted.PageCount;
        }

        // 2. Classify, unless the caller already said what it is.
        if (type is null)
        {
            try
            {
                type = await extractor.ClassifyAsync(text.Text, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning("Classification failed for document {DocumentId}: {ErrorType}", document.Id, ex.GetType().Name);
                return await FailAsync(document, DocumentType.Unknown, "ExtractorError", ct);
            }

            if (type is DocumentType.Unknown)
                return await FailAsync(document, DocumentType.Unknown, "UnknownDocumentType", ct);
        }

        // 3. Extract structured data.
        ExtractionResult result;
        var timer = Stopwatch.StartNew();
        try
        {
            result = await extractor.ExtractAsync(type.Value, text.Text, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning("Extraction failed for document {DocumentId}: {ErrorType}", document.Id, ex.GetType().Name);
            return await FailAsync(document, type.Value, "ExtractorError", ct);
        }
        timer.Stop();

        // 4. Validate, 5. persist.
        var issues = DocumentValidator.Validate(type.Value, result.Json);

        var extraction = new Extraction
        {
            DocumentId = document.Id,
            DocumentType = type.Value,
            Provider = extractor.Provider,
            Model = extractor.Model,
            PromptVersion = result.PromptVersion,
            Draft = result.Json,
            ValidationIssues = JsonSerializer.Serialize(issues, Json),
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            LatencyMs = timer.ElapsedMilliseconds,
            Outcome = ExtractionOutcome.Succeeded
        };
        db.Extractions.Add(extraction);

        document.DocumentType = type.Value;
        document.Status = issues.Count == 0 ? DocumentStatus.Extracted : DocumentStatus.NeedsReview;
        await db.SaveChangesAsync(ct);

        log.LogInformation(
            "Document {DocumentId} extracted as {DocumentType} by {Provider}: {IssueCount} issues, status {Status}, {LatencyMs} ms",
            document.Id, type.Value, extractor.Provider, issues.Count, document.Status, extraction.LatencyMs);

        return new ExtractionRun(ExtractionRunStatus.Completed, document, extraction, issues);
    }

    /// <summary>
    /// Atomically counts one extraction for today (UTC). The upsert only increments while the count is below the cap,
    /// so concurrent requests cannot push a user over it; zero affected rows means the cap is reached.
    /// </summary>
    private async Task<bool> TryChargeQuotaAsync(CancellationToken ct)
    {
        var cap = quota.Value.DailyExtractions;
        if (cap <= 0) return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UsageDays" ("OwnerId", "Day", "ExtractionCount")
            VALUES ({user.Id}, {today}, 1)
            ON CONFLICT ("OwnerId", "Day")
            DO UPDATE SET "ExtractionCount" = "UsageDays"."ExtractionCount" + 1
            WHERE "UsageDays"."ExtractionCount" < {cap}
            """, ct);
        return affected > 0;
    }

    private async Task<ExtractionRun> FailAsync(Document document, DocumentType type, string errorCode, CancellationToken ct)
    {
        var extraction = new Extraction
        {
            DocumentId = document.Id,
            DocumentType = type,
            Provider = extractor.Provider,
            Model = extractor.Model,
            PromptVersion = "none",
            Outcome = ExtractionOutcome.Failed,
            ErrorCode = errorCode
        };
        db.Extractions.Add(extraction);
        document.Status = DocumentStatus.Failed;
        await db.SaveChangesAsync(ct);

        log.LogWarning("Extraction failed for document {DocumentId}: {ErrorCode}", document.Id, errorCode);
        return new ExtractionRun(ExtractionRunStatus.Completed, document, extraction, []);
    }
}
