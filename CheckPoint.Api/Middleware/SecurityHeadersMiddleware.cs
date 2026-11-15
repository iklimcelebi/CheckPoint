namespace CheckPoint.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ApiContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private const string SwaggerContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        var isSwagger = context.Request.Path.StartsWithSegments("/swagger");
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers.ContentSecurityPolicy = isSwagger ? SwaggerContentSecurityPolicy : ApiContentSecurityPolicy;
            return Task.CompletedTask;
        });

        return next(context);
    }
}
