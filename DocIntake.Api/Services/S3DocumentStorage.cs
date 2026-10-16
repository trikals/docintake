using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using DocIntake.Api.Configuration;
using Microsoft.Extensions.Options;

namespace DocIntake.Api.Services;

public class S3DocumentStorage(
    IAmazonS3 s3,
    IOptions<StorageOptions> options,
    ILogger<S3DocumentStorage> logger) : IDocumentStorage
{
    private readonly string _bucket = options.Value.BucketName;

    public async Task UploadAsync(Guid storageId, Stream content, string contentType, CancellationToken ct = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = storageId.ToString(),
                InputStream = content,
                ContentType = contentType
            };
            await s3.PutObjectAsync(request, ct);

            logger.LogInformation("Stored blob {StorageId} in bucket {Bucket} as {ContentType}",
                storageId, _bucket, contentType);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning("S3 upload failed for {StorageId} in bucket {Bucket}: {StatusCode} {ErrorCode}",
                storageId, _bucket, ex.StatusCode, ex.ErrorCode);
            throw;
        }
    }

    public async Task<Stream?> OpenReadAsync(Guid storageId, CancellationToken ct = default)
    {
        try
        {
            var response = await s3.GetObjectAsync(_bucket, storageId.ToString(), ct);
            logger.LogDebug("Opened blob {StorageId} from bucket {Bucket}", storageId, _bucket);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogWarning("Blob {StorageId} not found in bucket {Bucket}", storageId, _bucket);
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning("S3 read failed for {StorageId} in bucket {Bucket}: {StatusCode} {ErrorCode}",
                storageId, _bucket, ex.StatusCode, ex.ErrorCode);
            throw;
        }
    }

    public async Task DeleteAsync(Guid storageId, CancellationToken ct = default)
    {
        try
        {
            await s3.DeleteObjectAsync(_bucket, storageId.ToString(), ct);
            logger.LogInformation("Deleted blob {StorageId} from bucket {Bucket}", storageId, _bucket);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning("S3 delete failed for {StorageId} in bucket {Bucket}: {StatusCode} {ErrorCode}",
                storageId, _bucket, ex.StatusCode, ex.ErrorCode);
            throw;
        }
    }
}