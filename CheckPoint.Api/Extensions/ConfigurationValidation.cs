using System.Globalization;

namespace CheckPoint.Api.Extensions;

public static class ConfigurationValidation
{
    public const int MinimumJwtKeyLength = 32;

    public static void ValidateRequiredSettings(IConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            errors.Add("ConnectionStrings:DefaultConnection zorunludur " +
                "(appsettings.Development.json içinde tanımlıdır; uygulamayı Development ortamında çalıştırın).");
        }

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < MinimumJwtKeyLength)
        {
            errors.Add($"Jwt:Key en az {MinimumJwtKeyLength} karakter olmalıdır " +
                "(README'deki 'dotnet user-secrets set \"Jwt:Key\" ...' adımıyla ayarlayın).");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
            errors.Add("Jwt:Issuer zorunludur.");
        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            errors.Add("Jwt:Audience zorunludur.");
        if (!IsPositiveInteger(configuration["Jwt:ExpiryMinutes"]))
            errors.Add("Jwt:ExpiryMinutes pozitif bir tam sayı olmalıdır.");

        if (string.IsNullOrWhiteSpace(configuration["FileStorage:RootPath"]))
            errors.Add("FileStorage:RootPath zorunludur.");
        if (!IsPositiveInteger(configuration["FileStorage:MaxFileSizeMB"]))
            errors.Add("FileStorage:MaxFileSizeMB pozitif bir tam sayı olmalıdır.");

        var maxRequestBodySize = configuration["Kestrel:Limits:MaxRequestBodySize"];
        if (maxRequestBodySize is not null &&
            (!long.TryParse(maxRequestBodySize, NumberStyles.None, CultureInfo.InvariantCulture, out var size) || size <= 0))
        {
            errors.Add("Kestrel:Limits:MaxRequestBodySize pozitif bir tam sayı olmalıdır.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Uygulama yapılandırması eksik veya geçersiz:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => $"- {error}")));
        }
    }

    private static bool IsPositiveInteger(string? value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0;
}
