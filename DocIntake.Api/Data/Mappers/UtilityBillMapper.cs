using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UtilityBillMapper : IEntityTypeConfiguration<UtilityBill>
{
    public void Configure(EntityTypeBuilder<UtilityBill> builder)
    {
        builder.ToTable("UtilityBills");
        builder.HasKey(u => u.Id);

        builder.HasOne(u => u.Document).WithOne().HasForeignKey<UtilityBill>(u => u.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.Charges).WithOne().HasForeignKey(c => c.UtilityBillId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(u => u.ProviderName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.UtilityType).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.AccountNumberMasked).HasMaxLength(20).IsRequired();
        builder.Property(u => u.ServiceAddress).HasMaxLength(300).IsRequired();
        builder.Property(u => u.UsageQuantity).HasPrecision(18, 4);
        builder.Property(u => u.UsageUnit).HasMaxLength(20).IsRequired();
        builder.Property(u => u.PreviousBalance).HasPrecision(18, 2);
        builder.Property(u => u.PaymentsReceived).HasPrecision(18, 2);
        builder.Property(u => u.CurrentCharges).HasPrecision(18, 2);
        builder.Property(u => u.AmountDue).HasPrecision(18, 2);

        builder.HasIndex(u => u.StatementDate);
    }
}

public class UtilityChargeMapper : IEntityTypeConfiguration<UtilityCharge>
{
    public void Configure(EntityTypeBuilder<UtilityCharge> builder)
    {
        builder.ToTable("UtilityCharges");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Description).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);

        builder.HasIndex(c => new { c.UtilityBillId, c.LineNumber }).IsUnique();
    }
}
