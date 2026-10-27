using System.Text.Json;
using DocIntake.Api.Data;
using DocIntake.Api.Models;
using DocIntake.Api.Services.Validation;
using Microsoft.EntityFrameworkCore;

namespace DocIntake.Api.Services;

public enum ConfirmStatus { NotFound, NotExtracted, InvalidData, IssuesNotAcknowledged, Confirmed }

public sealed record ConfirmRun(ConfirmStatus Status, Document? Document = null, IReadOnlyList<ValidationIssue>? Issues = null);

/// <summary>
/// Saves a person's edited data as the confirmed record for a document.
/// The data is validated again: the person may have introduced new errors.
/// Logs identifiers, types and counts only, never field values.
/// </summary>
public class DocumentConfirmationService(AppDbContext db, ILogger<DocumentConfirmationService> log)
{
    /// <summary>Problems with the shape of the data. These can never be acknowledged away: the data could not be saved.</summary>
    private static readonly HashSet<string> StructuralRules = ["InvalidJson", "MissingField", "InvalidValue"];

    public async Task<ConfirmRun> ConfirmAsync(int documentId, JsonElement data, bool acknowledgeIssues, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && !d.IsArchived, ct);
        if (document is null)
            return new ConfirmRun(ConfirmStatus.NotFound);

        if (document.DocumentType is null or DocumentType.Unknown)
        {
            log.LogWarning("Confirm refused: document {DocumentId} has not been extracted", documentId);
            return new ConfirmRun(ConfirmStatus.NotExtracted, document);
        }
        var type = document.DocumentType.Value;

        var issues = DocumentValidator.Validate(type, data.GetRawText());
        if (issues.Any(i => StructuralRules.Contains(i.Rule)))
        {
            log.LogWarning("Confirm rejected for document {DocumentId}: data is malformed ({IssueCount} issues)", documentId, issues.Count);
            return new ConfirmRun(ConfirmStatus.InvalidData, document, issues);
        }
        if (issues.Count > 0 && !acknowledgeIssues)
        {
            log.LogInformation("Confirm needs acknowledgement for document {DocumentId}: {IssueCount} issues", documentId, issues.Count);
            return new ConfirmRun(ConfirmStatus.IssuesNotAcknowledged, document, issues);
        }

        // Replace any earlier confirmation, then add the new one, in a single transaction.
        // Two saves because delete-then-insert of the same unique DocumentId must happen in that order.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await RemoveExistingAsync(type, documentId, ct);
        await db.SaveChangesAsync(ct);

        switch (type)
        {
            case DocumentType.Invoice: db.Invoices.Add(ConfirmedDataMapper.ToInvoice(documentId, data)); break;
            case DocumentType.BankStatement: db.BankStatements.Add(ConfirmedDataMapper.ToBankStatement(documentId, data)); break;
            case DocumentType.AchReturnNotice: db.AchReturnNotices.Add(ConfirmedDataMapper.ToAchReturnNotice(documentId, data)); break;
            case DocumentType.UtilityBill: db.UtilityBills.Add(ConfirmedDataMapper.ToUtilityBill(documentId, data)); break;
        }

        document.Status = DocumentStatus.Confirmed;
        document.ConfirmedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        log.LogInformation("Document {DocumentId} confirmed as {DocumentType} with {AcknowledgedIssues} acknowledged issues",
            documentId, type, issues.Count);
        return new ConfirmRun(ConfirmStatus.Confirmed, document, issues);
    }

    private async Task RemoveExistingAsync(DocumentType type, int documentId, CancellationToken ct)
    {
        switch (type)
        {
            case DocumentType.Invoice:
                var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.DocumentId == documentId, ct);
                if (invoice is not null) db.Invoices.Remove(invoice);
                break;
            case DocumentType.BankStatement:
                var statement = await db.BankStatements.Include(b => b.Transactions).FirstOrDefaultAsync(b => b.DocumentId == documentId, ct);
                if (statement is not null) db.BankStatements.Remove(statement);
                break;
            case DocumentType.AchReturnNotice:
                var notice = await db.AchReturnNotices.FirstOrDefaultAsync(a => a.DocumentId == documentId, ct);
                if (notice is not null) db.AchReturnNotices.Remove(notice);
                break;
            case DocumentType.UtilityBill:
                var bill = await db.UtilityBills.Include(u => u.Charges).FirstOrDefaultAsync(u => u.DocumentId == documentId, ct);
                if (bill is not null) db.UtilityBills.Remove(bill);
                break;
        }
    }
}
