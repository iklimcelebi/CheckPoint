using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Register_WithValidData_Returns201AsUserWithNormalizedEmail()
    {
        var response = await RegisterRawAsync(new
        {
            username = "ayse",
            email = "  Ayse@Test.Local ",
            password = DefaultPassword
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/me", response.Headers.Location?.AbsolutePath);
        var user = await ReadAsync<UserResponse>(response);
        Assert.Equal("ayse", user!.Username);
        Assert.Equal("ayse@test.local", user.Email);
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public async Task Register_WithRoleField_IgnoresRoleAndCreatesUser()
    {
        var response = await RegisterRawAsync(new
        {
            username = "mehmet",
            email = "mehmet@test.local",
            password = DefaultPassword,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("User", (await ReadAsync<UserResponse>(response))!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        await CreateUserAsync("ali");

        var response = await RegisterRawAsync(new
        {
            username = "baskaali",
            email = "ALI@test.local",
            password = DefaultPassword
        });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns409()
    {
        await CreateUserAsync("veli");

        var response = await RegisterRawAsync(new
        {
            username = "veli",
            email = "baska@test.local",
            password = DefaultPassword
        });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await RegisterRawAsync(new
        {
            username = "kisa",
            email = "kisa@test.local",
            password = "1234567"
        });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenThatWorksForMe()
    {
        var registered = await CreateUserAsync("zeynep");

        var login = await LoginAsync(registered.Email, DefaultPassword);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
        var me = await CreateClient(login.Token).GetFromJsonAsync<UserResponse>("/api/users/me", JsonOptions);
        Assert.Equal(registered.Id, me!.Id);
        Assert.Equal("zeynep", me.Username);
        Assert.Equal("User", me.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var user = await CreateUserAsync();

        var response = await LoginRawAsync(user.Email, "YanlisParola1");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await LoginRawAsync("yok@test.local", DefaultPassword);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }
}
