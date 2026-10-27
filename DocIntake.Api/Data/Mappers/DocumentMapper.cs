using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DocumentMapper : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.StorageId).IsRequired().HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.OwnerId).IsRequired().HasMaxLength(200);
        builder.Property(d => d.FileName).IsRequired();
        builder.Property(d => d.ContentType).IsRequired();
        builder.Property(d => d.IsArchived).HasDefaultValue(false);
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(d => d.FileType).HasDefaultValue(FileType.Unknown);
        builder.Property(d => d.PageCount).IsRequired(false);
        builder.Property(d => d.FileSize).IsRequired();
        builder.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(30).IsRequired(false);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(DocumentStatus.Received);
        builder.Property(d => d.ConfirmedAt).IsRequired(false);

        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.OwnerId, d.CreatedAt });
    }
}