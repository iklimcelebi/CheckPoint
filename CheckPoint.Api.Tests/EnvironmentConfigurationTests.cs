using System.Net;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class EnvironmentConfigurationTests : IAsyncLifetime
{
    private readonly CheckPointApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Theory]
    [InlineData("ConnectionStrings:DefaultConnection", "", "ConnectionStrings:DefaultConnection")]
    [InlineData("Jwt:Key", "", "Jwt:Key")]
    [InlineData("Jwt:Key", "kisa-anahtar", "Jwt:Key")]
    [InlineData("Jwt:Issuer", "", "Jwt:Issuer")]
    [InlineData("Jwt:ExpiryMinutes", "0", "Jwt:ExpiryMinutes")]
    [InlineData("FileStorage:RootPath", " ", "FileStorage:RootPath")]
    [InlineData("FileStorage:MaxFileSizeMB", "yirmi", "FileStorage:MaxFileSizeMB")]
    [InlineData("Kestrel:Limits:MaxRequestBodySize", "-1", "Kestrel:Limits:MaxRequestBodySize")]
    public void MissingOrInvalidSetting_StopsStartupWithTurkishMessage(string key, string value, string expectedKey)
    {
        _factory.Settings[key] = value;

        var exception = Assert.ThrowsAny<Exception>(() => _factory.CreateClient()).GetBaseException();

        Assert.Contains("Uygulama yapılandırması eksik veya geçersiz", exception.Message);
        Assert.Contains(expectedKey, exception.Message);
    }

    [Theory]
    [InlineData("kisaP1")]
    [InlineData("sadeceharfler")]
    [InlineData("1234567890")]
    public async Task SeedAdminPasswordViolatingPolicy_StopsStartup(string password)
    {
        await _factory.CreateDatabaseAsync();
        _factory.Settings["SeedAdmin:Password"] = password;

        var message = Assert.ThrowsAny<Exception>(() => _factory.CreateClient()).GetBaseException().Message;

        Assert.Contains("SeedAdmin:Password parola kuralına uymuyor", message);
        Assert.DoesNotContain(password, message);
    }

    [Fact]
    public void SeveralInvalidSettings_AreReportedTogether()
    {
        _factory.Settings["ConnectionStrings:DefaultConnection"] = "";
        _factory.Settings["Jwt:Key"] = "";

        var message = Assert.ThrowsAny<Exception>(() => _factory.CreateClient()).GetBaseException().Message;

        Assert.Contains("ConnectionStrings:DefaultConnection", message);
        Assert.Contains("Jwt:Key", message);
    }

    [Fact]
    public async Task Development_WithAutoMigrate_AppliesMigrationsAndServesSwagger()
    {
        _factory.EnvironmentName = "Development";
        _factory.Settings["Database:AutoMigrate"] = "true";
        Assert.False(await _factory.DatabaseExistsAsync());

        var client = _factory.CreateClient();

        Assert.True(await _factory.DatabaseExistsAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        var swagger = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Contains("'unsafe-inline'", Assert.Single(swagger.Headers.GetValues("Content-Security-Policy")));
    }

    [Theory]
    [InlineData("Development", "false")]
    [InlineData("Testing", "true")]
    [InlineData("Production", "true")]
    public async Task AutoMigrate_RunsOnlyInDevelopmentWhenEnabled(string environment, string autoMigrate)
    {
        _factory.EnvironmentName = environment;
        _factory.Settings["Database:AutoMigrate"] = autoMigrate;

        try
        {
            _factory.CreateClient();
        }
        catch (Exception)
        {
        }

        Assert.False(await _factory.DatabaseExistsAsync());
    }

    [Fact]
    public async Task Production_DisablesSwaggerAndIgnoresSeedAdminFromNonEnvironmentSources()
    {
        await _factory.CreateDatabaseAsync();
        _factory.EnvironmentName = "Production";

        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await using var db = _factory.CreateDbContext();
        Assert.False(db.Users.Any(user => user.Role == "Admin"));
    }
}

[CollectionDefinition(nameof(ProcessEnvironmentCollection), DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection;

[Collection(nameof(ProcessEnvironmentCollection))]
public sealed class ProductionSeedAdminTests : IAsyncLifetime
{
    private readonly CheckPointApiFactory _factory = new();

    public async Task InitializeAsync() => await _factory.CreateDatabaseAsync();

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("SeedAdmin__Email", null);
        Environment.SetEnvironmentVariable("SeedAdmin__Password", null);
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Production_CreatesSeedAdminFromEnvironmentVariables()
    {
        _factory.EnvironmentName = "Production";
        _factory.Settings["SeedAdmin:Email"] = null;
        _factory.Settings["SeedAdmin:Password"] = null;
        Environment.SetEnvironmentVariable("SeedAdmin__Email", _factory.AdminEmail);
        Environment.SetEnvironmentVariable("SeedAdmin__Password", _factory.AdminPassword);

        _factory.CreateClient();

        await using var db = _factory.CreateDbContext();
        var admin = Assert.Single(db.Users.Where(user => user.Role == "Admin"));
        Assert.Equal(_factory.AdminEmail, admin.Email);
    }
}
