using CheckPoint.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(
        AppDbContext db,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(user => user.Role == "Admin", cancellationToken))
            return;

        var email = configuration["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (password.Length < 8)
            throw new InvalidOperationException("SeedAdmin:Password must contain at least 8 characters.");

        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new InvalidOperationException(
                "The configured SeedAdmin:Email is already registered to a non-admin user.");

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
