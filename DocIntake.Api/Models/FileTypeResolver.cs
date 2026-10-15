namespace DocIntake.Api.Models;

public static class FileTypeResolver
{
    public static FileType Resolve(string? contentType, string? fileName = null)
    {
        // Content-Type can arrive as "text/plain; charset=utf-8", so drop the parameters
        var mime = contentType?.Split(';')[0].Trim().ToLowerInvariant();

        var fromMime = mime switch
        {
            "application/pdf" => FileType.Pdf,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => FileType.Docx,
            "application/msword" => FileType.Doc,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => FileType.Xlsx,
            "application/vnd.ms-excel" => FileType.Xls,
            "text/plain" => FileType.Text,
            _ => FileType.Unknown
        };

        if (fromMime != FileType.Unknown)
            return fromMime;

        // Clients often send application/octet-stream, so fall back to the extension
        return Path.GetExtension(fileName)?.ToLowerInvariant() switch
        {
            ".pdf" => FileType.Pdf,
            ".docx" => FileType.Docx,
            ".doc" => FileType.Doc,
            ".xlsx" => FileType.Xlsx,
            ".xls" => FileType.Xls,
            ".txt" => FileType.Text,
            _ => FileType.Unknown
        };
    }
}