using System.Security.Claims;
using DocIntake.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DocIntake.Api.Configuration;

public static class AuthServiceExtensions
{
    /// <summary>
    /// JWT bearer validation. Issuer, audience and authority come from configuration
    /// (Authentication:Schemes:Bearer:*), so swapping identity providers is a configuration change:
    /// dotnet user-jwts locally, Cognito on AWS, Entra ID on Azure. With nothing configured, every request is rejected.
    /// </summary>
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keep claim names as they appear in the token ("sub", "scope") instead of remapping them.
                options.MapInboundClaims = false;

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // The exception type only: never the token or its contents.
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                            .CreateLogger("DocIntake.Api.Authentication")
                            .LogWarning("Token rejected: {ErrorType}", context.Exception.GetType().Name);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.Read, p => p.RequireAssertion(c => HasScope(c.User, auth.ScopeClaim, AuthPolicies.Read)))
            .AddPolicy(AuthPolicies.Write, p => p.RequireAssertion(c => HasScope(c.User, auth.ScopeClaim, AuthPolicies.Write)));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    private static bool HasScope(ClaimsPrincipal user, string scopeClaim, string scope) =>
        user.FindAll(scopeClaim).SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Contains(scope);
}
