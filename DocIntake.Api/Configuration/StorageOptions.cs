namespace DocIntake.Api.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-2";

    // Local only: points the SDK at S3Mock. Leave empty in AWS.
    public string? ServiceUrl { get; set; }

    // Local only: dummy keys for S3Mock. In AWS the task's IAM role is used instead.
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
}