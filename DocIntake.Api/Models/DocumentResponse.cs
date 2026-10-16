namespace DocIntake.Api.Models;

public record DocumentResponse(int Id, string FileName, string ContentType, FileType FileType, long FileSize, DateTime CreatedAt)
{
    public static DocumentResponse From(Document d) =>
        new(d.Id, d.FileName, d.ContentType, d.FileType, d.FileSize, d.CreatedAt);
}
