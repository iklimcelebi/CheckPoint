using System.Text.Json;
using CheckPoint.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CheckPoint.Api.Middleware;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            StorageUnavailableException storage => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Hizmet kullanılamıyor",
                Detail = storage.Message
            },
            BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } => new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "İstek çok büyük",
                Detail = "İstek gövdesi izin verilen boyutu aşıyor."
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Geçersiz istek",
                Detail = "İstek okunamadı."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Sunucu hatası",
                Detail = "İstek işlenirken beklenmeyen bir hata oluştu."
            }
        };
        problem.Instance = httpContext.Request.Path;

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "An unexpected error occurred while processing the request.");
        else if (exception is StorageUnavailableException)
            logger.LogError(exception, "File storage is unavailable.");
        else
            logger.LogWarning("Rejected a malformed or oversized request with status {StatusCode}.", problem.Status);

        httpContext.Response.StatusCode = problem.Status!.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            cancellationToken: cancellationToken);

        return true;
    }
}
