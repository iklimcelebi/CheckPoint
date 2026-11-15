using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CheckPoint.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadSizeLimitAttribute : Attribute, IAsyncResourceFilter, IOrderedFilter
{
    public const long BytesPerMegabyte = 1024 * 1024;
    public const long FormOverheadBytes = BytesPerMegabyte;
    public const string TooLargeMessage = "Dosya izin verilen boyut sınırını aşıyor.";

    public int Order => int.MinValue;

    public static long GetRequestLimit(IConfiguration configuration) =>
        configuration.GetValue<int>("FileStorage:MaxFileSizeMB") * BytesPerMegabyte + FormOverheadBytes;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var requestLimit = GetRequestLimit(httpContext.RequestServices.GetRequiredService<IConfiguration>());

        var sizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = requestLimit;

        if (httpContext.Request.ContentLength > requestLimit)
        {
            context.Result = CreateProblem(httpContext, StatusCodes.Status413PayloadTooLarge, TooLargeMessage);
            return;
        }

        if (httpContext.Request.HasFormContentType)
        {
            httpContext.Features.Set<IFormFeature>(new FormFeature(
                httpContext.Request,
                new FormOptions { MultipartBodyLengthLimit = requestLimit }));

            try
            {
                await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
            }
            catch (BadHttpRequestException exception)
                when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
            {
                context.Result = CreateProblem(httpContext, StatusCodes.Status413PayloadTooLarge, TooLargeMessage);
                return;
            }
            catch (InvalidDataException exception)
                when (exception.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = CreateProblem(httpContext, StatusCodes.Status413PayloadTooLarge, TooLargeMessage);
                return;
            }
            catch (InvalidDataException)
            {
                context.Result = CreateProblem(httpContext, StatusCodes.Status400BadRequest, "Form verisi okunamadı.");
                return;
            }
        }

        await next();
    }

    private static ObjectResult CreateProblem(HttpContext httpContext, int statusCode, string detail)
    {
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(httpContext, statusCode, detail: detail);
        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
