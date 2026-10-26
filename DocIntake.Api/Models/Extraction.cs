namespace DocIntake.Api.Models;

/// <summary>One extraction attempt for a document: what was asked, which model answered, and what came back.</summary>
public class Extraction
{
    public int Id { get; set; }

    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public DocumentType DocumentType { get; set; }

    /// <summary>Which implementation produced it: Stub, Bedrock or Foundry.</summary>
    public string Provider { get; set; } = null!;
    public string Model { get; set; } = null!;

    /// <summary>Prompt file version used, e.g. invoice.v1, so results stay comparable.</summary>
    public string PromptVersion { get; set; } = null!;

    /// <summary>The model's JSON answer, untouched. Null when the attempt failed.</summary>
    public string? Draft { get; set; }

    /// <summary>JSON array of validation issues (rule + field path). Null when the attempt failed.</summary>
    public string? ValidationIssues { get; set; }

    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public long LatencyMs { get; set; }

    public ExtractionOutcome Outcome { get; set; }

    /// <summary>Short machine-readable reason when the attempt failed. Never contains document content.</summary>
    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
