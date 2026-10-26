using DocIntake.Api.Services;

namespace DocIntake.Api.Configuration;

public static class TextExtractionServiceExtensions
{
    public static IServiceCollection AddTextExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TextExtractionOptions>(configuration.GetSection("TextExtraction"));
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
        return services;
    }
}
