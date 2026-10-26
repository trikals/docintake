namespace DocIntake.Api.Services;

/// <summary>The document's text could not be read. The message is safe to show or log: it never contains document content.</summary>
public class TextExtractionException : Exception
{
    public TextExtractionException(string message) : base(message) { }
    public TextExtractionException(string message, Exception inner) : base(message, inner) { }
}
