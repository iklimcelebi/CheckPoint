using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CheckPoint.Api.Extensions;

public static class SecurityServiceExtensions
{
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue<int>("RateLimiting:Auth:PermitLimit");
        var windowSeconds = configuration.GetValue<int>("RateLimiting:Auth:WindowSeconds");
        if (permitLimit <= 0 || windowSeconds <= 0)
        {
            throw new InvalidOperationException(
                "RateLimiting:Auth:PermitLimit ve RateLimiting:Auth:WindowSeconds pozitif sayılar olmalıdır.");
        }

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    httpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = httpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Detail = "Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin."
                        }
                    });
            };

            options.AddPolicy(AuthRateLimitPolicy, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0
                }));
        });
    }

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(NormalizeOrigin)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return services.AddCors(options => options.AddDefaultPolicy(policy =>
        {
            if (origins.Length == 0)
                return;

            policy
                .WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Authorization", "Content-Type")
                .WithExposedHeaders("Content-Disposition", "Retry-After")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }));
    }

    private static string NormalizeOrigin(string origin)
    {
        var trimmed = origin.Trim().TrimEnd('/');
        if (trimmed.Contains('*') ||
            !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.PathAndQuery != "/" ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException(
                $"Cors:AllowedOrigins içinde geçersiz değer: '{origin}'. Her değer joker karakter içermeyen " +
                "tam bir http(s) adresi olmalıdır (örn. http://localhost:5173).");
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }
}
