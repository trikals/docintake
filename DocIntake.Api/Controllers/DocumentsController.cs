using DocIntake.Api.Configuration;
using DocIntake.Api.Data;
using DocIntake.Api.Models;
using DocIntake.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace DocIntake.Api.Controllers;

[ApiController]
[Authorize]
[Route("documents")]
public class DocumentsController(
    AppDbContext db,
    ICurrentUser currentUser,
    IDocumentStorage storage,
    DocumentExtractionService extraction,
    DocumentConfirmationService confirmation,
    DocumentDetailService detail,
    ILogger<DocumentsController> log) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>Upload a PDF, DOCX or TXT file (multipart form field "file").</summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
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

        var document = Document.Create(currentUser.Id, Path.GetFileName(file.FileName), file.ContentType, file.Length);
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

    /// <summary>List documents, newest first. pageSize is capped at 50.</summary>
    [HttpGet]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType<PagedResponse<DocumentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DocumentResponse>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Documents.AsNoTracking().OwnedBy(currentUser.Id).Where(d => !d.IsArchived);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        log.LogInformation("Listed documents: page {Page}, size {PageSize}, returned {Count} of {Total}", page, pageSize, items.Count, total);
        return Ok(new PagedResponse<DocumentResponse>(items.Select(DocumentResponse.From).ToList(), page, pageSize, total));
    }

    /// <summary>Get one document with its latest extraction (draft and issues) and, once confirmed, its confirmed data.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType<DocumentDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await detail.GetAsync(id, ct);
        if (result is null)
        {
            log.LogWarning("Document {DocumentId} not found", id);
            return Problem("Document not found.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }

    /// <summary>Stream the original file (shown inline, so a PDF opens in the browser's viewer).</summary>
    [HttpGet("{id:int}/file")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int id, CancellationToken ct)
    {
        var document = await db.Documents.AsNoTracking().OwnedBy(currentUser.Id).FirstOrDefaultAsync(d => d.Id == id && !d.IsArchived, ct);
        if (document is null)
        {
            log.LogWarning("File requested for missing document {DocumentId}", id);
            return Problem("Document not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var stream = await storage.OpenReadAsync(document.StorageId, ct);
        if (stream is null)
        {
            // Row exists but the blob is gone: data inconsistency worth noticing.
            log.LogError("Blob {StorageId} missing for document {DocumentId}", document.StorageId, id);
            return Problem("The stored file could not be found.", statusCode: StatusCodes.Status404NotFound);
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        log.LogInformation("Streaming file for document {DocumentId} ({Size} bytes)", id, document.FileSize);
        return File(stream, document.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Delete the document row and its stored file.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var document = await db.Documents.OwnedBy(currentUser.Id).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null)
        {
            log.LogWarning("Delete requested for missing document {DocumentId}", id);
            return Problem("Document not found.", statusCode: StatusCodes.Status404NotFound);
        }

        // Row first: a leftover blob is invisible and expires by lifecycle rule,
        // but a row pointing at a missing blob would be a visible, broken document.
        db.Documents.Remove(document);
        await db.SaveChangesAsync(ct);

        try
        {
            await storage.DeleteAsync(document.StorageId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Document {DocumentId} deleted but blob {StorageId} could not be removed", id, document.StorageId);
        }

        log.LogInformation("Document {DocumentId} deleted", id);
        return NoContent();
    }

    /// <summary>
    /// Run extraction: read the text, classify (unless documentType is given), extract, validate.
    /// Always returns 200 with the outcome in the body; a failed attempt has outcome Failed and an errorCode.
    /// </summary>
    [HttpPost("{id:int}/extract")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType<ExtractionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Extract(
        int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ExtractRequest? request,
        CancellationToken ct)
    {
        var run = await extraction.RunAsync(id, request?.DocumentType, ct);

        IActionResult result = run.Status switch
        {
            ExtractionRunStatus.NotFound => Problem("Document not found.", statusCode: StatusCodes.Status404NotFound),
            ExtractionRunStatus.AlreadyConfirmed => Problem("This document is already confirmed and cannot be extracted again.", statusCode: StatusCodes.Status409Conflict),
            _ => Ok(ExtractionResponse.From(run))
        };
        return result;
    }

    /// <summary>
    /// Save the person's edited data as the confirmed record. The data is validated again.
    /// Malformed data returns 400. Remaining validation issues return 409 until acknowledgeIssues is true.
    /// </summary>
    [HttpPost("{id:int}/confirm")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType<ConfirmResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ConfirmIssuesResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ConfirmIssuesResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmRequest request, CancellationToken ct)
    {
        if (request.Data.ValueKind != System.Text.Json.JsonValueKind.Object)
            return Problem("'data' must be a JSON object.", statusCode: StatusCodes.Status400BadRequest);

        var run = await confirmation.ConfirmAsync(id, request.Data, request.AcknowledgeIssues, ct);

        IActionResult result = run.Status switch
        {
            ConfirmStatus.NotFound => Problem("Document not found.", statusCode: StatusCodes.Status404NotFound),
            ConfirmStatus.NotExtracted => Problem("Run extraction on this document before confirming it.", statusCode: StatusCodes.Status409Conflict),
            ConfirmStatus.InvalidData => BadRequest(new ConfirmIssuesResponse("The data is incomplete or malformed and cannot be saved.", run.Issues!)),
            ConfirmStatus.IssuesNotAcknowledged => Conflict(new ConfirmIssuesResponse("Validation issues remain. Fix them, or resend with acknowledgeIssues set to true.", run.Issues!)),
            _ => Ok(new ConfirmResponse(DocumentResponse.From(run.Document!), run.Issues!))
        };
        return result;
    }
}
