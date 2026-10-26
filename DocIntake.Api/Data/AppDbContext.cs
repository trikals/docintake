using DocIntake.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocIntake.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentText> DocumentTexts { get; set; } = null!;
    public DbSet<Extraction> Extractions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DocumentMapper());
        modelBuilder.ApplyConfiguration(new DocumentTextMapper());
        modelBuilder.ApplyConfiguration(new ExtractionMapper());
    }
}