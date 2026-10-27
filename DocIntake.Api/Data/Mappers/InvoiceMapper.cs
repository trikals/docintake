using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class InvoiceMapper : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.HasOne(i => i.Document).WithOne().HasForeignKey<Invoice>(i => i.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(i => i.Lines).WithOne().HasForeignKey(l => l.InvoiceId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.VendorName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.InvoiceNumber).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(10).IsRequired();
        builder.Property(i => i.BillTo).HasMaxLength(200);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.Tax).HasPrecision(18, 2);
        builder.Property(i => i.Total).HasPrecision(18, 2);

        builder.HasIndex(i => i.InvoiceDate);
    }
}

public class InvoiceLineMapper : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Description).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.Amount).HasPrecision(18, 2);

        builder.HasIndex(l => new { l.InvoiceId, l.LineNumber }).IsUnique();
    }
}
