using DocIntake.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace DocIntake.Tests.Integration;

/// <summary>
/// Starts the real API in memory against a throwaway Postgres container.
/// Real: migrations, EF queries, controllers, authentication, authorization, validation.
/// Replaced: S3 storage (in memory) and the token signing key (a test key).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // Environment variables are read when the app builds its configuration, before any
        // test override could be applied, so this is the reliable way to point it at the container.
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Database__MigrateOnStartup", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" has no appsettings file of its own, so nothing from a developer machine leaks in.
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDocumentStorage, InMemoryDocumentStorage>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = TestJwt.Issuer,
                    ValidAudience = TestJwt.Audience,
                    IssuerSigningKey = TestJwt.Key,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "sub"
                };
            });
        });
    }

    Task IAsyncLifetime.DisposeAsync() => _db.DisposeAsync().AsTask();
}
