using DocIntake.Api.Data;
using DocIntake.Api.Models;
using DocIntake.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocIntake.Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController(
    AppDbContext db,
    IDocumentStorage storage,
    ILogger<DocumentsController> log) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>Upload a PDF, DOCX or TXT file (multipart form field "file").</summary>
    [HttpPost]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            log.LogWarning("Upload rejected: empty or missing file");
            return Problem("A non-empty file is required.", statusCode: StatusCodes.Status400BadRequest);
        }
        if (file.Length > MaxFileSize)
        {
            log.LogWarning("Upload rejected: too large ({Size} bytes)", file.Length);
            return Problem("File exceeds the 10 MB limit.", statusCode: StatusCodes.Status413PayloadTooLarge);
        }
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            log.LogWarning("Upload rejected: no file name");
            return Problem("File name is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var document = Document.Create(Path.GetFileName(file.FileName), file.ContentType, file.Length);
        if (document.FileType is not (FileType.Pdf or FileType.Docx or FileType.Text))
        {
            log.LogWarning("Upload rejected: unsupported type {ContentType}", file.ContentType);
            return Problem("Unsupported file type. Use PDF, DOCX or TXT.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        // Blob first, then the row. If the row fails, remove the blob so nothing is orphaned.
        await using (var stream = file.OpenReadStream())
        {
            await storage.UploadAsync(document.StorageId, stream, document.ContentType, ct);
        }

        try
        {
            db.Documents.Add(document);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            log.LogError("Saving document row failed; removing blob {StorageId}", document.StorageId);
            await storage.DeleteAsync(document.StorageId, CancellationToken.None);
            throw; // global handler returns the generic 500
        }

        log.LogInformation("Document {DocumentId} uploaded: {FileType}, {Size} bytes", document.Id, document.FileType, document.FileSize);
        return Created($"/documents/{document.Id}", DocumentResponse.From(document));
    }
}
