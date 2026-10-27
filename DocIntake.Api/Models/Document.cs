
namespace DocIntake.Api.Models;

public class Document
{
    public int Id { get; set; }
    
    /// <summary>
    /// Document's unqiue identifier for blob storage and retrieval.
    /// </summary>
    public Guid StorageId { get; set; } = Guid.NewGuid();
    /// <summary>The signed-in user's id (token sub claim). Every query is filtered by it.</summary>
    public string OwnerId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
//    public byte[] Content { get; set; } = null!;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FileType FileType { get; set; } = FileType.Unknown;
    public int? PageCount { get; set; }
    public long FileSize { get; set; }

    /// <summary>Null until classified by the model or chosen by the user.</summary>
    public DocumentType? DocumentType { get; set; }

    public DocumentStatus Status { get; set; } = DocumentStatus.Received;

    /// <summary>When a person confirmed the extracted data.</summary>
    public DateTime? ConfirmedAt { get; set; }

    public static Document Create(string ownerId, string fileName, string contentType, long fileSize) => new()
    {
        OwnerId = ownerId,
        FileName = fileName,
        ContentType = contentType,
        FileSize = fileSize,
        FileType = FileTypeResolver.Resolve(contentType, fileName)
    };
}