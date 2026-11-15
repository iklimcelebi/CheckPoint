using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpLogging;

namespace CheckPoint.Api.Observability;

public sealed class RequestLoggingInterceptor : IHttpLoggingInterceptor
{
    public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext) => ValueTask.CompletedTask;

    public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
    {
        var httpContext = logContext.HttpContext;
        logContext.AddParameter("TraceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
            logContext.AddParameter("UserId", userId);

        return ValueTask.CompletedTask;
    }
}
