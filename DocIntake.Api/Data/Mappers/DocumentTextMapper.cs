using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DocumentTextMapper : IEntityTypeConfiguration<DocumentText>
{
    public void Configure(EntityTypeBuilder<DocumentText> builder)
    {
        builder.ToTable("DocumentTexts");

        builder.HasKey(t => t.DocumentId);

        // One-to-one with Document; deleting a document deletes its text.
        builder.HasOne(t => t.Document)
            .WithOne()
            .HasForeignKey<DocumentText>(t => t.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Text).IsRequired();
        builder.Property(t => t.Method).IsRequired().HasMaxLength(20);
        builder.Property(t => t.PageCount).IsRequired(false);
        builder.Property(t => t.CharCount).IsRequired();
        builder.Property(t => t.Truncated).HasDefaultValue(false);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()");
    }
}
