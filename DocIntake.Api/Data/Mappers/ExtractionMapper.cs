using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ExtractionMapper : IEntityTypeConfiguration<Extraction>
{
    public void Configure(EntityTypeBuilder<Extraction> builder)
    {
        builder.ToTable("Extractions");

        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.DocumentType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Provider).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Model).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PromptVersion).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Draft).HasColumnType("jsonb");
        builder.Property(e => e.ValidationIssues).HasColumnType("jsonb");
        builder.Property(e => e.Outcome).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ErrorCode).HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(e => e.DocumentId);
    }
}
