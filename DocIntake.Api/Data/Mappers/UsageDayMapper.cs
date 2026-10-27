using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UsageDayMapper : IEntityTypeConfiguration<UsageDay>
{
    public void Configure(EntityTypeBuilder<UsageDay> builder)
    {
        builder.ToTable("UsageDays");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.OwnerId).IsRequired().HasMaxLength(200);

        // One row per user per day. The atomic upsert in DocumentExtractionService relies on this index.
        builder.HasIndex(u => new { u.OwnerId, u.Day }).IsUnique();
    }
}
