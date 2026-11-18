using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CheckPoint.Api.Tests;

public sealed class ChangePasswordTests : ApiTestBase
{
    private const string NewPassword = "YeniParola456";

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_Returns204AndOnlyNewPasswordWorks()
    {
        var user = await CreateUserAsync("parolaci");

        var response = await user.Client.PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = DefaultPassword, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginRawAsync(user.Email, DefaultPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync(user.Email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ForAdmin_Works()
    {
        var admin = await CreateAdminAsync();

        var response = await admin.Client.PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = Factory.AdminPassword, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var login = await LoginAsync(Factory.AdminEmail, NewPassword);
        Assert.Equal("Admin", login.User.Role);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_Returns400OnCurrentPasswordField()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = "YanlisParola1", newPassword = NewPassword });

        await AssertValidationErrorAsync(response, "Mevcut parola hatalı.");
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync(user.Email, DefaultPassword)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_UsesCurrentPasswordKey()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = "YanlisParola1", newPassword = NewPassword });

        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Equal(["Mevcut parola hatalı."], problem!.Errors["CurrentPassword"]);
    }

    [Theory]
    [InlineData("sadeceharf", "Parola en az bir harf ve bir rakam içermelidir.")]
    [InlineData("Kisa1", "Parola 8 ile 64 karakter arasında olmalıdır.")]
    [InlineData(DefaultPassword, "Yeni parola mevcut paroladan farklı olmalıdır.")]
    public async Task ChangePassword_WithInvalidNewPassword_Returns400(string newPassword, string message)
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = DefaultPassword, newPassword });

        await AssertValidationErrorAsync(response, message);
        Assert.Equal(HttpStatusCode.OK, (await LoginRawAsync(user.Email, DefaultPassword)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await CreateClient().PutAsJsonAsync(
            "/api/users/me/password",
            new { currentPassword = DefaultPassword, newPassword = NewPassword });

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_IsRateLimitedLikeLogin()
    {
        Factory.Settings["RateLimiting:Auth:PermitLimit"] = "3";
        var user = await CreateUserAsync();

        HttpResponseMessage last = new();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            last = await user.Client.PutAsJsonAsync(
                "/api/users/me/password",
                new { currentPassword = "YanlisParola1", newPassword = NewPassword });
        }

        await AssertProblemAsync(last, HttpStatusCode.TooManyRequests);
    }
}
