using DocIntake.Api.Models;

namespace DocIntake.Api.Data;

public static class DocumentQueryExtensions
{
    /// <summary>Only the given user's documents. Every document query goes through this.</summary>
    public static IQueryable<Document> OwnedBy(this IQueryable<Document> documents, string ownerId) =>
        documents.Where(d => d.OwnerId == ownerId);
}
