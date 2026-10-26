namespace DocIntake.Api.Configuration;

public class ExtractionOptions
{
    /// <summary>Which IDocumentExtractor to use: Stub now; Bedrock (AWS) and Foundry (Azure) later.</summary>
    public string Provider { get; set; } = "Stub";
}
