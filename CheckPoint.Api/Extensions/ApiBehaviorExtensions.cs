using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace CheckPoint.Api.Extensions;

public static class ApiBehaviorExtensions
{
    public const string ValidationTitle = "Bir veya daha fazla doğrulama hatası oluştu.";
    private const string InvalidValueMessage = "Değer geçersiz veya hatalı biçimde.";

    private static readonly Dictionary<int, string> ProblemTitles = new()
    {
        [StatusCodes.Status400BadRequest] = "Geçersiz istek",
        [StatusCodes.Status401Unauthorized] = "Kimlik doğrulaması gerekli",
        [StatusCodes.Status403Forbidden] = "Erişim reddedildi",
        [StatusCodes.Status404NotFound] = "Bulunamadı",
        [StatusCodes.Status405MethodNotAllowed] = "Yönteme izin verilmiyor",
        [StatusCodes.Status406NotAcceptable] = "Kabul edilemez",
        [StatusCodes.Status408RequestTimeout] = "İstek zaman aşımına uğradı",
        [StatusCodes.Status409Conflict] = "Çakışma",
        [StatusCodes.Status412PreconditionFailed] = "Ön koşul sağlanamadı",
        [StatusCodes.Status413PayloadTooLarge] = "İstek çok büyük",
        [StatusCodes.Status415UnsupportedMediaType] = "Desteklenmeyen içerik türü",
        [StatusCodes.Status422UnprocessableEntity] = "İşlenemeyen içerik",
        [StatusCodes.Status429TooManyRequests] = "Çok fazla istek",
        [StatusCodes.Status500InternalServerError] = "Sunucu hatası",
        [StatusCodes.Status503ServiceUnavailable] = "Hizmet kullanılamıyor"
    };

    public static void UseTurkishTitles(this ProblemDetailsContext context)
    {
        var statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
        if (ProblemTitles.TryGetValue(statusCode, out var title))
            context.ProblemDetails.Title = title;
    }

    public static void UseTurkishModelBindingMessages(this DefaultModelBindingMessageProvider provider)
    {
        provider.SetAttemptedValueIsInvalidAccessor((_, field) => $"{field} alanı için girilen değer geçersiz.");
        provider.SetMissingBindRequiredValueAccessor(field => $"{field} alanı zorunludur.");
        provider.SetMissingKeyOrValueAccessor(() => "Değer zorunludur.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi zorunludur.");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => InvalidValueMessage);
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => InvalidValueMessage);
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "Değer sayı olmalıdır.");
        provider.SetUnknownValueIsInvalidAccessor(field => $"{field} alanı için girilen değer geçersiz.");
        provider.SetValueIsInvalidAccessor(_ => InvalidValueMessage);
        provider.SetValueMustBeANumberAccessor(field => $"{field} alanı sayı olmalıdır.");
        provider.SetValueMustNotBeNullAccessor(_ => "Değer boş olamaz.");
    }

    public static void UseTurkishProblemResponses(this ApiBehaviorOptions options)
    {
        foreach (var (statusCode, title) in ProblemTitles)
        {
            if (options.ClientErrorMapping.TryGetValue(statusCode, out var data))
                data.Title = title;
            else
                options.ClientErrorMapping[statusCode] = new ClientErrorData { Title = title };
        }

        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrEmpty(error.ErrorMessage)
                            ? InvalidValueMessage
                            : error.ErrorMessage)
                        .ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ValidationTitle,
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    }
}
