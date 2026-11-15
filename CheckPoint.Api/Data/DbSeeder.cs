using CheckPoint.Api.Models;
using CheckPoint.Api.Validation;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Data;

public static class DbSeeder
{
    private const string EmailKey = "SeedAdmin:Email";
    private const string PasswordKey = "SeedAdmin:Password";

    public static async Task SeedAdminAsync(
        AppDbContext db,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var email = configuration[EmailKey]?.Trim().ToLowerInvariant();
        var password = configuration[PasswordKey];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
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
