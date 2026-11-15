using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class UniversityTests : ApiTestBase
{
    [Fact]
    public async Task Crud_AsAdmin_WorksEndToEnd()
    {
        var admin = await CreateAdminAsync();

        var create = await admin.Client.PostAsJsonAsync(
            "/api/universities",
            new { name = "  Pamukkale Üniversitesi ", city = " Denizli " });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await ReadAsync<UniversityResponse>(create))!;
        Assert.Equal("Pamukkale Üniversitesi", created.Name);
        Assert.Equal("Denizli", created.City);
        Assert.EndsWith($"/api/Universities/{created.Id}", create.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);

        var fetched = await admin.Client.GetFromJsonAsync<UniversityResponse>(
            $"/api/universities/{created.Id}", JsonOptions);
        Assert.Equal(created, fetched);

        var update = await admin.Client.PutAsJsonAsync(
            $"/api/universities/{created.Id}",
            new { name = "Pamukkale Üni", city = "Denizli" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var list = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>("/api/universities", JsonOptions);
        Assert.Contains(list!.Items, university => university.Id == created.Id && university.Name == "Pamukkale Üni");

        var delete = await admin.Client.DeleteAsync($"/api/universities/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await admin.Client.GetAsync($"/api/universities/{created.Id}");
        await AssertProblemAsync(afterDelete, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_AsUser_Returns200SortedByName()
    {
        var admin = await CreateAdminAsync();
        await CreateUniversityAsync(admin.Client, "Zirve Üniversitesi");
        await CreateUniversityAsync(admin.Client, "Akdeniz Üniversitesi");
        var user = await CreateUserAsync();

        var list = await user.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>("/api/universities", JsonOptions);

        Assert.Equal(["Akdeniz Üniversitesi", "Zirve Üniversitesi"], list!.Items.Select(university => university.Name));
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409()
    {
        var admin = await CreateAdminAsync();
        await CreateUniversityAsync(admin.Client, "Ege Üniversitesi");

        var response = await admin.Client.PostAsJsonAsync(
            "/api/universities",
            new { name = "Ege Üniversitesi", city = "İzmir" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ToExistingName_Returns409()
    {
        var admin = await CreateAdminAsync();
        await CreateUniversityAsync(admin.Client, "Ege Üniversitesi");
        var other = await CreateUniversityAsync(admin.Client, "Dokuz Eylül Üniversitesi");

        var response = await admin.Client.PutAsJsonAsync(
            $"/api/universities/{other.Id}",
            new { name = "Ege Üniversitesi", city = "İzmir" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var admin = await CreateAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/universities", new { name = "", city = "Denizli" });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_UniversityWithCourses_Returns409AndKeepsUniversity()
    {
        var admin = await CreateAdminAsync();
        var (university, _) = await CreateCatalogAsync(admin);

        var response = await admin.Client.DeleteAsync($"/api/universities/{university.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        var stillThere = await admin.Client.GetAsync($"/api/universities/{university.Id}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task GetUpdateDelete_Missing_Return404()
    {
        var admin = await CreateAdminAsync();

        await AssertProblemAsync(await admin.Client.GetAsync("/api/universities/999999"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await admin.Client.PutAsJsonAsync("/api/universities/999999", new { name = "Yok", city = "Yok" }),
            HttpStatusCode.NotFound);
        await AssertProblemAsync(await admin.Client.DeleteAsync("/api/universities/999999"), HttpStatusCode.NotFound);
    }
}
