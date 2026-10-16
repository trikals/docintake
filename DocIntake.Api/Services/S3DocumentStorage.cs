using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using DocIntake.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocIntake.Api.Services;

public class S3DocumentStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IDocumentStorage
{
    private readonly string _bucket = options.Value.BucketName;

    public async Task UploadAsync(Guid storageId, Stream content, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageId.ToString(),
            InputStream = content,
            ContentType = contentType
        };
        await s3.PutObjectAsync(request, ct);
    }

    public async Task<Stream?> OpenReadAsync(Guid storageId, CancellationToken ct = default)
    {
        try
        {
            var response = await s3.GetObjectAsync(_bucket, storageId.ToString(), ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(Guid storageId, CancellationToken ct = default)
        => s3.DeleteObjectAsync(_bucket, storageId.ToString(), ct);
}