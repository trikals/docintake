using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BankStatementMapper : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("BankStatements");
        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.Document).WithOne().HasForeignKey<BankStatement>(b => b.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(b => b.Transactions).WithOne().HasForeignKey(t => t.BankStatementId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.InstitutionName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.AccountHolder).HasMaxLength(200).IsRequired();
        builder.Property(b => b.AccountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.AccountNumberMasked).HasMaxLength(20).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(10).IsRequired();
        builder.Property(b => b.OpeningBalance).HasPrecision(18, 2);
        builder.Property(b => b.ClosingBalance).HasPrecision(18, 2);

        builder.HasIndex(b => b.PeriodEnd);

        // Lookup for "other statements for the same account" (not unique: nothing guarantees uniqueness).
        builder.HasIndex(b => new { b.InstitutionName, b.AccountHolder, b.AccountType, b.AccountNumberMasked });
    }
}

public class BankTransactionMapper : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.ToTable("BankTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Direction).HasConversion<string>().HasMaxLength(10);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Ignore(t => t.SignedAmount);

        builder.HasIndex(t => new { t.BankStatementId, t.LineNumber }).IsUnique();
    }
}
