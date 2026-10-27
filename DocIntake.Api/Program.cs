using DocIntake.Api.Configuration;
using DocIntake.Api.Data;
using DocIntake.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
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
    builder.Services.AddAppAuthentication(builder.Configuration);
    builder.Services.AddAppRateLimiting(builder.Configuration);
    builder.Services.AddDocumentStorage(builder.Configuration);
    builder.Services.AddTextExtraction(builder.Configuration);
    builder.Services.AddDocumentExtractor(builder.Configuration);
    builder.Services.AddScoped<DocumentConfirmationService>();
    builder.Services.AddScoped<DocumentDetailService>();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

    var app = builder.Build();

    // One summary line per request. Successful /health calls are hidden because the
    // load balancer hits them every few seconds; failed ones still show.
    app.UseSerilogRequestLogging(options =>
    {
        // The caller's id on the request summary line; resolved at the end of the request, after authentication.
        options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
            diagnostics.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");

        options.GetLevel = (httpContext, elapsedMs, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;

            return httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Verbose
                : LogEventLevel.Information;
        };
    });

    app.UseExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    // Every log line written while handling a request carries the caller's id.
    app.Use(async (httpContext, next) =>
    {
        using (LogContext.PushProperty("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous"))
            await next();
    });

    if (app.Environment.IsDevelopment())
    {
        // TEMPORARY: used to test the exception handler, then delete
        //app.MapGet("/boom", () => { throw new InvalidOperationException("Test failure"); });        
        app.MapOpenApi();

    }

    app.MapHealthChecks("/health");

    app.MapControllers();

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