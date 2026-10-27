using System.Text.Json;

namespace DocIntake.Api.Models;

/// <summary>Body for POST /documents/{id}/confirm.</summary>
/// <param name="Data">The edited data, shaped like the document type's schema.</param>
/// <param name="AcknowledgeIssues">Set to true to save even though validation issues remain.</param>
public record ConfirmRequest(JsonElement Data, bool AcknowledgeIssues = false);
