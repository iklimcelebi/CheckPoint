using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheckPoint.Api.Tests;

public sealed class ObservabilityTests : ApiTestBase
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_AreAnonymousAndReturnOnlyStatus(string url)
    {
        var response = await CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task ReadinessCheck_WhenDatabaseIsUnreachable_Returns503WithoutDetails()
    {
        Factory.Settings["ConnectionStrings:DefaultConnection"] =
            $"Server=(localdb)\\MSSQLLocalDB;Database=CheckPointMissing_{Guid.NewGuid():N};" +
            "Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3";
        Factory.Settings["SeedAdmin:Email"] = "";
        var client = CreateClient();

        var live = await client.GetAsync("/health");
        var ready = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RequestLog_ContainsMethodPathStatusDurationAndUserId()
    {
        var logs = new CapturingLoggerProvider();
        Factory.ConfigureServices = services => services.AddSingleton<ILoggerProvider>(logs);
        var user = await CreateUserAsync();

        await user.Client.GetAsync("/api/users/me?gizliSorgu=deger");

        var entry = Assert.Single(logs.Logs, log =>
            log.Category.EndsWith("HttpLoggingMiddleware") && log.Value("Path")?.ToString() == "/api/users/me");
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("GET", entry.Value("Method")?.ToString());
        Assert.Equal("200", entry.Value("StatusCode")?.ToString());
        Assert.Equal(user.Id.ToString(), entry.Value("UserId")?.ToString());
        Assert.NotNull(entry.Value("TraceId"));
        Assert.NotNull(entry.Value("Duration"));
        Assert.DoesNotContain("gizliSorgu", entry.AllText);
    }

    [Fact]
    public async Task Logs_NeverContainPasswordsEmailsTokensOrKeys()
    {
        var logs = new CapturingLoggerProvider();
        Factory.ConfigureServices = services => services.AddSingleton<ILoggerProvider>(logs);
        const string password = "CokGizliParola987";
        const string email = "kisisel.veri@test.local";
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        await RegisterRawAsync(new { username = "kisiselveri", email, password });
        var login = await LoginAsync(email, password);
        await LoginRawAsync(email, "YanlisParola000");
        await LoginRawAsync("olmayan.kisi@test.local", password);
        var client = CreateClient(login.Token);
        await client.GetAsync("/api/users/me");
        await UploadAsync(client, university.Id, course.Id);
        await CreateClient(login.Token + "bozuk").GetAsync("/api/users/me");
        await client.PostAsJsonAsync("/api/universities", new { name = "Yetkisiz", city = "Denizli" });

        Assert.NotEmpty(logs.Logs);
        var everything = string.Join(Environment.NewLine, logs.Logs.Select(log => log.AllText));
        Assert.DoesNotContain(password, everything);
        Assert.DoesNotContain("YanlisParola000", everything);
        Assert.DoesNotContain(email, everything, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("olmayan.kisi", everything);
        Assert.DoesNotContain(Factory.AdminEmail, everything, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Factory.AdminPassword, everything);
        Assert.DoesNotContain(Factory.JwtKey, everything);
        Assert.DoesNotContain(login.Token, everything);
        Assert.DoesNotContain(login.Token.Split('.')[1], everything);
        Assert.Contains(logs.Logs, log => log.Message == "Seed admin account created.");
    }
}
