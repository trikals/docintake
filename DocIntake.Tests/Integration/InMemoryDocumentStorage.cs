using System.Collections.Concurrent;
using DocIntake.Api.Services;

namespace DocIntake.Tests.Integration;

/// <summary>Stands in for S3 during tests, so they need only a database.</summary>
public class InMemoryDocumentStorage : IDocumentStorage
{
    private readonly ConcurrentDictionary<Guid, byte[]> _blobs = new();

    public async Task UploadAsync(Guid storageId, Stream content, string contentType, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        _blobs[storageId] = buffer.ToArray();
    }

    public Task<Stream?> OpenReadAsync(Guid storageId, CancellationToken ct = default) =>
        Task.FromResult<Stream?>(_blobs.TryGetValue(storageId, out var bytes) ? new MemoryStream(bytes) : null);

    public Task DeleteAsync(Guid storageId, CancellationToken ct = default)
    {
        _blobs.TryRemove(storageId, out _);
        return Task.CompletedTask;
    }
}
