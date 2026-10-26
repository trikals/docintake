using System.Text.Json;
using DocIntake.Api.Services;
using DocIntake.Api.Services.Validation;

namespace DocIntake.Api.Models;

public record ExtractionResponse(
    int ExtractionId,
    int DocumentId,
    DocumentStatus DocumentStatus,
    DocumentType DocumentType,
    string Provider,
    string Model,
    string PromptVersion,
    ExtractionOutcome Outcome,
    string? ErrorCode,
    long LatencyMs,
    JsonElement? Draft,
    IReadOnlyList<ValidationIssue> Issues,
    DateTime CreatedAt)
{
    public static ExtractionResponse From(ExtractionRun run)
    {
        var e = run.Extraction!;
        JsonElement? draft = e.Draft is null ? null : JsonDocument.Parse(e.Draft).RootElement.Clone();
        return new ExtractionResponse(e.Id, e.DocumentId, run.Document!.Status, e.DocumentType, e.Provider, e.Model,
            e.PromptVersion, e.Outcome, e.ErrorCode, e.LatencyMs, draft, run.Issues ?? [], e.CreatedAt);
    }
}
