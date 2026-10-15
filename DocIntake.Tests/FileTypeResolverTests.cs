using DocIntake.Api.Models;

namespace DocIntake.Tests;

public class FileTypeResolverTests
{
    [Theory]
    [InlineData("application/pdf", "a.pdf", FileType.Pdf)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "a.docx", FileType.Docx)]
    [InlineData("text/plain; charset=utf-8", "a.txt", FileType.Text)]
    [InlineData("application/octet-stream", "report.XLSX", FileType.Xlsx)]
    [InlineData("application/octet-stream", "notes.bin", FileType.Unknown)]
    public void Resolve_maps_content_type_or_extension(string contentType, string fileName, FileType expected)
        => Assert.Equal(expected, FileTypeResolver.Resolve(contentType, fileName));
}