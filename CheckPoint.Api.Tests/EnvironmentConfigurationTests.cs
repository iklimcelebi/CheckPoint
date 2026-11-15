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
        var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        using var document = System.Text.Json.JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.Equal("integer", schemas.GetProperty("PagedResponseOfUniversityResponse")
            .GetProperty("properties").GetProperty("totalCount").GetProperty("type").GetString());
        Assert.Equal("integer", schemas.GetProperty("UniversityResponse")
            .GetProperty("properties").GetProperty("id").GetProperty("type").GetString());
        var swagger = await client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
        Assert.Contains("'unsafe-inline'", Assert.Single(swagger.Headers.GetValues("Content-Security-Policy")));
    }

    [Theory]
    [InlineData("Development", "false")]
    [InlineData("Testing", "true")]
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
    public async Task OutsideDevelopment_SwaggerIsDisabled()
    {
        await _factory.CreateDatabaseAsync();

        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }
}
