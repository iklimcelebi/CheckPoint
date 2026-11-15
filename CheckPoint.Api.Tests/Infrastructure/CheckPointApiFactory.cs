using System.Security.Cryptography;
using CheckPoint.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheckPoint.Api.Tests.Infrastructure;

public sealed class CheckPointApiFactory : WebApplicationFactory<Program>
{
    private readonly string _id = Guid.NewGuid().ToString("N");

    public CheckPointApiFactory()
    {
        DatabaseName = $"CheckPointTest_{_id}";
        ConnectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";
        UploadRoot = Path.Combine(Path.GetTempPath(), "CheckPointTests", _id);
        JwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        AdminEmail = $"admin{_id[..8]}@test.local";
        AdminPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
    }

    public string DatabaseName { get; }
    public string ConnectionString { get; }
    public string UploadRoot { get; }
    public string JwtKey { get; }
    public string AdminEmail { get; }
    public string AdminPassword { get; }

    public Dictionary<string, string?> Settings { get; } = new()
    {
        ["RateLimiting:Auth:PermitLimit"] = "100000"
    };

    public Action<IServiceCollection>? ConfigureServices { get; set; }

    public async Task CreateDatabaseAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("FileStorage:RootPath", UploadRoot);
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);
        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        if (ConfigureServices is not null)
            builder.ConfigureTestServices(ConfigureServices);
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await using (var db = CreateDbContext())
            {
                await db.Database.EnsureDeletedAsync();
            }

            if (Directory.Exists(UploadRoot))
                Directory.Delete(UploadRoot, recursive: true);
        }
    }
}
