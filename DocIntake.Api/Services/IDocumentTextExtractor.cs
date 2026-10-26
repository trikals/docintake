namespace DocIntake.Api.Services;

/// <summary>Text pulled from a document, ready to send to the model.</summary>
/// <param name="Method">How it was read (PdfPig, OpenXml, PlainText); stored with the text.</param>
/// <param name="PageCount">Total pages in the source, when the format has pages.</param>
/// <param name="Truncated">True when the page or character cap cut the text short.</param>
public sealed record ExtractedText(string Text, string Method, int? PageCount, bool Truncated);

public interface IDocumentTextExtractor
{
    /// <exception cref="TextExtractionException">The file is unsupported, corrupt, or unreadable.</exception>
    Task<ExtractedText> ExtractAsync(Stream content, FileType fileType, CancellationToken ct = default);
}
