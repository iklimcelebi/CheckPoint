using CheckPoint.Api.Models;
using CheckPoint.Api.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace CheckPoint.Api.Data;

public static class DbSeeder
{
    private const string EmailKey = "SeedAdmin:Email";
    private const string PasswordKey = "SeedAdmin:Password";

    public static async Task SeedAdminAsync(
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var (email, password) = ReadSeedSettings(configuration, environment, logger);
        if (email is null || password is null)
            return;

        if (await db.Users.AnyAsync(user => user.Role == "Admin", cancellationToken))
            return;

        if (PasswordPolicy.Validate(password) is { } passwordError)
            throw new InvalidOperationException($"SeedAdmin:Password parola kuralına uymuyor: {passwordError}");

        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new InvalidOperationException(
                "SeedAdmin:Email ile kayıtlı, Admin olmayan bir kullanıcı zaten var.");
        }

        var username = await GetAvailableUsernameAsync(db, email, cancellationToken);
        db.Users.Add(new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin",
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed admin account created.");
    }

    private static (string? Email, string? Password) ReadSeedSettings(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger)
    {
        var email = configuration[EmailKey];
        var password = configuration[PasswordKey];

        if (environment.IsProduction())
        {
            var fromEnvironment = configuration is IConfigurationRoot root
                ? root.Providers.OfType<EnvironmentVariablesConfigurationProvider>().ToList()
                : [];
            var environmentEmail = ReadFrom(fromEnvironment, EmailKey);
            var environmentPassword = ReadFrom(fromEnvironment, PasswordKey);

            if ((!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password)) &&
                (environmentEmail is null || environmentPassword is null))
            {
                logger.LogWarning(
                    "Seed admin settings were ignored. In Production they must be provided as environment variables.");
            }

            email = environmentEmail;
            password = environmentPassword;
        }

        email = email?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            ? (null, null)
            : (email, password);
    }

    private static string? ReadFrom(IEnumerable<EnvironmentVariablesConfigurationProvider> providers, string key)
    {
        string? result = null;
        foreach (var provider in providers)
        {
            if (provider.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value))
                result = value;
        }

        return result;
    }

    private static async Task<string> GetAvailableUsernameAsync(
        AppDbContext db,
        string email,
        CancellationToken cancellationToken)
    {
        var baseUsername = email.Split('@', 2)[0].Trim();
        if (baseUsername.Length < 3)
            baseUsername = "admin";

        baseUsername = baseUsername[..Math.Min(baseUsername.Length, 50)];
        var candidate = baseUsername;
        var suffix = 1;

        while (await db.Users.AnyAsync(user => user.Username == candidate, cancellationToken))
        {
            var suffixText = suffix.ToString();
            suffix++;
            candidate = $"{baseUsername[..Math.Min(baseUsername.Length, 50 - suffixText.Length)]}{suffixText}";
        }

        return candidate;
    }
}
