namespace DocIntake.Api.Services.Validation;

/// <summary>A problem found in extracted data.</summary>
/// <param name="Rule">Stable rule name, e.g. TotalMismatch. Tests and the UI key off this.</param>
/// <param name="Path">Field the issue is about, e.g. lineItems[0].amount.</param>
/// <param name="Message">Human-readable explanation shown in the review screen.</param>
public sealed record ValidationIssue(string Rule, string Path, string Message);
