using System.Text.Json;

namespace DocIntake.Api.Models;

/// <summary>Everything the review screen needs for one document.</summary>
/// <param name="LatestExtraction">The most recent extraction attempt (draft, issues, outcome), or null if none yet.</param>
/// <param name="ConfirmedData">The confirmed record shaped like the type's schema, or null until confirmed.</param>
public record DocumentDetailResponse(
    DocumentResponse Document,
    ExtractionResponse? LatestExtraction,
    JsonElement? ConfirmedData,
    DateTime? ConfirmedAt);
