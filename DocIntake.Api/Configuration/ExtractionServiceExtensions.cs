using DocIntake.Api.Services;

namespace DocIntake.Api.Configuration;

public static class ExtractionServiceExtensions
{
    public static IServiceCollection AddDocumentExtractor(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("Extraction").Get<ExtractionOptions>() ?? new ExtractionOptions();

        switch (options.Provider)
        {
            case "Stub":
                services.AddSingleton<IDocumentExtractor, StubDocumentExtractor>();
                break;
            // case "Bedrock": ... (week 7)
            // case "Foundry": ... (Azure port)
            default:
                throw new InvalidOperationException($"Unknown Extraction:Provider '{options.Provider}'.");
        }

        services.Configure<QuotaOptions>(configuration.GetSection("Quotas"));
        services.AddScoped<DocumentExtractionService>();
        return services;
    }
}
