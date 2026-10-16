namespace DocIntake.Api.Services;

public interface IDocumentStorage
{
    Task UploadAsync(Guid storageId, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Returns null if no blob exists for this id.</summary>
    Task<Stream?> OpenReadAsync(Guid storageId, CancellationToken ct = default);

    Task DeleteAsync(Guid storageId, CancellationToken ct = default);
}