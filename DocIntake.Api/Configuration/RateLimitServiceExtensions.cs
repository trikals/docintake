using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocIntake.Api.Configuration;

public class RateLimitOptions
{
    /// <summary>Requests per minute per user on the upload and extract endpoints.</summary>
    public int PermitsPerMinute { get; set; } = 10;
}

public static class RateLimitServiceExtensions
{
    public const string HeavyPolicy = "heavy";

    /// <summary>
    /// Per-user rate limit for the expensive endpoints (upload, extract). Anonymous callers share a bucket per IP address.
    /// Over the limit: 429 with a Retry-After header.
    /// </summary>
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection("RateLimit").Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(HeavyPolicy, httpContext =>
            {
                var key = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, ct) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DocIntake.Api.RateLimit")
                    .LogWarning("Rate limit hit on {Method} {Path}", http.Request.Method, http.Request.Path.Value);

                await http.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "You are sending requests too quickly. Wait a moment and try again."
                }, ct);
            };
        });

        return services;
    }
}
