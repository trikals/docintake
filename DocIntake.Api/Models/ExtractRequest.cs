namespace DocIntake.Api.Models;

/// <summary>Optional body for POST /documents/{id}/extract. Leave DocumentType out to let the model classify.</summary>
public record ExtractRequest(DocumentType? DocumentType);
