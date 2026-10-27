using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AchReturnNoticeMapper : IEntityTypeConfiguration<AchReturnNotice>
{
    public void Configure(EntityTypeBuilder<AchReturnNotice> builder)
    {
        builder.ToTable("AchReturnNotices");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Document).WithOne().HasForeignKey<AchReturnNotice>(a => a.DocumentId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.OriginatorName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.ReceiverName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.RdfiName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.SecCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.TraceNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.OriginalTraceNumber).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Amount).HasPrecision(18, 2);
        builder.Property(a => a.ReturnReasonCode).HasMaxLength(10).IsRequired();
        builder.Property(a => a.ReturnReasonDescription).HasMaxLength(200).IsRequired();

        builder.HasIndex(a => a.ReturnDate);
    }
}
