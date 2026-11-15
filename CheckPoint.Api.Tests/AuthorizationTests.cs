using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class AuthorizationTests : ApiTestBase
{
    [Theory]
    [InlineData("/api/users/me")]
    [InlineData("/api/users/me/resources")]
    [InlineData("/api/universities")]
    [InlineData("/api/courses")]
    [InlineData("/api/resources")]
    [InlineData("/api/resources/1/download")]
    public async Task ProtectedGet_WithoutToken_Returns401(string url)
    {
        var response = await CreateClient().GetAsync(url);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedGet_WithInvalidToken_Returns401()
    {
        var response = await CreateClient("gecersiz.token.degeri").GetAsync("/api/universities");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UniversityPost_AsUser_Returns403()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/universities",
            new { name = "Ege Üniversitesi", city = "İzmir" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UniversityPost_AsAdmin_Returns201()
    {
        var admin = await CreateAdminAsync();

        var response = await admin.Client.PostAsJsonAsync(
            "/api/universities",
            new { name = "Ege Üniversitesi", city = "İzmir" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UniversityPutAndDelete_AsUser_Return403()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        var user = await CreateUserAsync();

        var put = await user.Client.PutAsJsonAsync(
            $"/api/universities/{university.Id}",
            new { name = "Yeni Ad", city = "İzmir" });
        var delete = await user.Client.DeleteAsync($"/api/universities/{university.Id}");

        await AssertProblemAsync(put, HttpStatusCode.Forbidden);
        await AssertProblemAsync(delete, HttpStatusCode.Forbidden);
    }
}
