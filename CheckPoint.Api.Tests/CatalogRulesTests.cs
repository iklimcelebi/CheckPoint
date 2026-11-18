using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;
using CheckPoint.Api.Validation;

namespace CheckPoint.Api.Tests;

public sealed class CatalogRulesTests : ApiTestBase
{
    [Fact]
    public void ProvinceList_HasAll81DistinctProvinces()
    {
        Assert.Equal(81, TurkishProvinces.All.Count);
        Assert.Equal(81, TurkishProvinces.All.Distinct().Count());
    }

    [Theory]
    [InlineData("istanbul", "İstanbul")]
    [InlineData("  IZMIR ", "İzmir")]
    [InlineData("şanlıurfa", "Şanlıurfa")]
    [InlineData("Iğdır", "Iğdır")]
    [InlineData("eskisehir", "Eskişehir")]
    public async Task CreateUniversity_StoresCanonicalProvinceName(string city, string expected)
    {
        var admin = await CreateAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/universities", new { name = NextName("Üniversite "), city });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(expected, (await ReadAsync<UniversityResponse>(response))!.City);
    }

    [Theory]
    [InlineData("istenbul")]
    [InlineData("Paris")]
    public async Task CreateAndUpdateUniversity_RejectUnknownProvince(string city)
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);

        await AssertValidationErrorAsync(
            await admin.Client.PostAsJsonAsync("/api/universities", new { name = "Yeni Üniversite", city }),
            TurkishProvinces.InvalidMessage);
        await AssertValidationErrorAsync(
            await admin.Client.PutAsJsonAsync($"/api/universities/{university.Id}", new { name = university.Name, city }),
            TurkishProvinces.InvalidMessage);
    }

    [Fact]
    public async Task RegularUser_CanAddCourseButCannotEditIt()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        var user = await CreateUserAsync();

        var course = await CreateCourseAsync(user.Client, university.Id, "KULLANICI1");

        await AssertProblemAsync(
            await user.Client.PutAsJsonAsync($"/api/courses/{course.Id}", new { name = "Değişti", code = "X1" }),
            HttpStatusCode.Forbidden);
        var unchanged = await user.Client.GetFromJsonAsync<CourseResponse>($"/api/courses/{course.Id}", JsonOptions);
        Assert.Equal(("Veri Yapıları", "KULLANICI1"), (unchanged!.Name, unchanged.Code));

        var byAdmin = await admin.Client.PutAsJsonAsync($"/api/courses/{course.Id}", new { name = "Değişti", code = "X1" });
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
    }
}
