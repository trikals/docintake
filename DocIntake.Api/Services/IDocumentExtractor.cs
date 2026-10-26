namespace DocIntake.Api.Services;

/// <summary>The model's answer for one document.</summary>
/// <param name="Json">Structured data matching the type's schema, as JSON text.</param>
/// <param name="PromptVersion">Which prompt produced it, e.g. invoice.v1.</param>
public sealed record ExtractionResult(string Json, string PromptVersion, int? InputTokens, int? OutputTokens);

/// <summary>
/// Turns document text into a document type and structured data.
/// Implementations: Stub (canned answers), Bedrock (AWS), Foundry (Azure).
/// </summary>
public interface IDocumentExtractor
{
    /// <summary>Provider name stored with every extraction, e.g. Stub, Bedrock, Foundry.</summary>
    string Provider { get; }

    /// <summary>Model identifier stored with every extraction.</summary>
    string Model { get; }

    Task<DocumentType> ClassifyAsync(string text, CancellationToken ct = default);

    Task<ExtractionResult> ExtractAsync(DocumentType documentType, string text, CancellationToken ct = default);
}
