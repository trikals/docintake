using DocIntake.Api.Configuration;
using DocIntake.Api.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Bootstrap logger: captures failures that happen before the host is built
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Configuration sources, lowest to highest priority: appsettings.json,
    // appsettings.{Environment}.json, user secrets (Development), environment variables, command line.
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddDocumentStorage(builder.Configuration);
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // One summary line per request. Successful /health calls are hidden because the
    // load balancer hits them every few seconds; failed ones still show.
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsedMs, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;

            return httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapHealthChecks("/health");

    // Document endpoints will be mapped here next

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;