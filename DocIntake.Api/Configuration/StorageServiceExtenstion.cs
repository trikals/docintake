using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using DocIntake.Api.Services;
using Microsoft.Extensions.Options;

namespace DocIntake.Api.Configuration;

public static class StorageServiceExtensions
{
    public static IServiceCollection AddDocumentStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

            var config = new AmazonS3Config
            {
                // Newer SDK versions add checksums by default; only send them when required
                // so S3-compatible local stores don't trip over them.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };

            if (!string.IsNullOrEmpty(o.ServiceUrl))
            {
                config.ServiceURL = o.ServiceUrl;
                config.AuthenticationRegion = o.Region;
                config.ForcePathStyle = true;   // http://host:9090/bucket/key instead of bucket.host
            }
            else
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(o.Region);
            }

            return string.IsNullOrEmpty(o.AccessKey)
                ? new AmazonS3Client(config)    // real AWS: default credential chain (IAM role)
                : new AmazonS3Client(new BasicAWSCredentials(o.AccessKey, o.SecretKey), config);
        });

        services.AddSingleton<IDocumentStorage, S3DocumentStorage>();
        return services;
    }
}