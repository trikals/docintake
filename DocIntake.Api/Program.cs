using DocIntake.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration: CreateBuilder has already loaded, in increasing priority:
//   appsettings.json -> appsettings.{Environment}.json -> user secrets (Development)
//   -> environment variables -> command-line arguments.
// In AWS the connection string will arrive as the environment variable
// ConnectionStrings__Default, which overrides the file value below.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

// Services
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

// Document endpoints will be mapped here next

app.Run();

// Makes the Program class visible to integration tests later
public partial class Program;