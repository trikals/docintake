namespace DocIntake.Api.Configuration;

public class TextExtractionOptions
{
    /// <summary>Pages read from a PDF; later pages are ignored (cost and abuse control).</summary>
    public int MaxPages { get; set; } = 20;

    /// <summary>Characters kept from any document; the rest is cut.</summary>
    public int MaxCharacters { get; set; } = 60_000;
}
