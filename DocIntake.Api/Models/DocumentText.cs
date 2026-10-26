namespace DocIntake.Api.Models;

/// <summary>
/// The text read from a document: exactly what the model sees. One row per document.
/// Contains document content, so it is never logged.
/// </summary>
public class DocumentText
{
    /// <summary>Primary key and foreign key: one text per document.</summary>
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    public string Text { get; set; } = null!;

    /// <summary>How it was read: PdfPig, OpenXml or PlainText.</summary>
    public string Method { get; set; } = null!;

    /// <summary>Pages in the source, for formats that have pages.</summary>
    public int? PageCount { get; set; }

    public int CharCount { get; set; }

    /// <summary>True when the page or character cap cut the text short.</summary>
    public bool Truncated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
