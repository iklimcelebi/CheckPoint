using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Tests;

public sealed class UserLifecycleTests : ApiTestBase
{
    [Fact]
    public async Task TokenOfDeletedUser_Returns401()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var user = await CreateUserAsync();
        await using (var db = Factory.CreateDbContext())
        {
            await db.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();
        }

        await AssertProblemAsync(await user.Client.GetAsync("/api/users/me"), HttpStatusCode.Unauthorized);
        await AssertProblemAsync(
            await UploadRawAsync(user.Client, university.Id, course.Id, TestFiles.Text()),
            HttpStatusCode.Unauthorized);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task TokenIssuedBeforeRoleChange_Returns401_AndNewLoginUsesNewRole()
    {
        var user = await CreateUserAsync();
        await using (var db = Factory.CreateDbContext())
        {
            await db.Users
                .Where(candidate => candidate.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(candidate => candidate.Role, "Admin"));
        }

        await AssertProblemAsync(await user.Client.GetAsync("/api/universities"), HttpStatusCode.Unauthorized);

        var login = await LoginAsync(user.Email, DefaultPassword);
        Assert.Equal("Admin", login.User.Role);
        var response = await CreateClient(login.Token).PostAsJsonAsync(
            "/api/universities", new { name = "Yeni Üniversite", city = "Ankara" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task InvalidTokenOnAnonymousEndpoint_DoesNotBlockLogin()
    {
        var user = await CreateUserAsync();
        var client = CreateClient("gecersiz.token.degeri");

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = DefaultPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
