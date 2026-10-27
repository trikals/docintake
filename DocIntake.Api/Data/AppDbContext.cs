using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocIntake.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentText> DocumentTexts { get; set; } = null!;
    public DbSet<Extraction> Extractions { get; set; } = null!;
    public DbSet<UsageDay> UsageDays { get; set; } = null!;

    // Confirmed data, one set per document type
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;
    public DbSet<BankStatement> BankStatements { get; set; } = null!;
    public DbSet<BankTransaction> BankTransactions { get; set; } = null!;
    public DbSet<AchReturnNotice> AchReturnNotices { get; set; } = null!;
    public DbSet<UtilityBill> UtilityBills { get; set; } = null!;
    public DbSet<UtilityCharge> UtilityCharges { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration<T> in Data/Mappers
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
