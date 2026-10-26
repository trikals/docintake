using System.Diagnostics;
using System.Text;
using DocIntake.Api.Configuration;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace DocIntake.Api.Services;

/// <summary>
/// Reads text in-process: PdfPig for PDFs with a text layer, the Open XML SDK for DOCX, plain reading for TXT.
/// Scanned images need OCR and are not handled here.
/// Logs only the file type, sizes and timings, never the text.
/// </summary>
public sealed class DocumentTextExtractor(
    IOptions<TextExtractionOptions> options,
    ILogger<DocumentTextExtractor> log) : IDocumentTextExtractor
{
    private readonly TextExtractionOptions _options = options.Value;

    public async Task<ExtractedText> ExtractAsync(Stream content, FileType fileType, CancellationToken ct = default)
    {
        var timer = Stopwatch.StartNew();
        try
        {
            // Parsers need a seekable stream, and S3 response streams are not. Files are capped at 10 MB.
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var (text, method, pageCount, pagesCut) = fileType switch
            {
                FileType.Pdf => ReadPdf(buffer),
                FileType.Docx => ReadDocx(buffer),
                FileType.Text => ReadPlainText(buffer),
                _ => throw new TextExtractionException($"Text extraction does not support {fileType} files.")
            };

            text = text.Replace("\r\n", "\n").Trim();
            var charsCut = text.Length > _options.MaxCharacters;
            if (charsCut) text = text[.._options.MaxCharacters];

            var result = new ExtractedText(text, method, pageCount, pagesCut || charsCut);
            log.LogInformation(
                "Extracted text from {FileType} via {Method}: {Characters} characters, {PageCount} pages, truncated {Truncated}, {ElapsedMs} ms",
                fileType, method, result.Text.Length, pageCount, result.Truncated, timer.ElapsedMilliseconds);
            return result;
        }
        catch (TextExtractionException ex)
        {
            log.LogWarning("Text extraction rejected {FileType}: {Reason}", fileType, ex.Message);
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Only the exception type is logged: parser messages are not guaranteed to be free of content.
            log.LogWarning("Text extraction failed for {FileType}: {ErrorType}", fileType, ex.GetType().Name);
            throw new TextExtractionException($"The {fileType} file could not be read.", ex);
        }
    }

    private (string Text, string Method, int? PageCount, bool PagesCut) ReadPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages().Take(_options.MaxPages))
        {
            sb.AppendLine(ContentOrderTextExtractor.GetText(page));
            sb.AppendLine();
        }
        return (sb.ToString(), "PdfPig", pdf.NumberOfPages, pdf.NumberOfPages > _options.MaxPages);
    }

    private static (string Text, string Method, int? PageCount, bool PagesCut) ReadDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body
            ?? throw new TextExtractionException("The Word document has no body.");

        var sb = new StringBuilder();
        foreach (var element in body.ChildElements)
        {
            switch (element)
            {
                case Paragraph p:
                    sb.AppendLine(p.InnerText);
                    break;
                case Table table:
                    foreach (var row in table.Elements<TableRow>())
                        sb.AppendLine(string.Join(" | ", row.Elements<TableCell>().Select(c => c.InnerText)));
                    break;
            }
        }
        return (sb.ToString(), "OpenXml", null, false);
    }

    private static (string Text, string Method, int? PageCount, bool PagesCut) ReadPlainText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return (reader.ReadToEnd(), "PlainText", null, false);
    }
}
