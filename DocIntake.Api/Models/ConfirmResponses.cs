using DocIntake.Api.Services.Validation;

namespace DocIntake.Api.Models;

/// <summary>Returned on success. AcknowledgedIssues lists any issues the person chose to accept.</summary>
public record ConfirmResponse(DocumentResponse Document, IReadOnlyList<ValidationIssue> AcknowledgedIssues);

/// <summary>Returned with 400 (data malformed) or 409 (issues need acknowledging).</summary>
public record ConfirmIssuesResponse(string Message, IReadOnlyList<ValidationIssue> Issues);
