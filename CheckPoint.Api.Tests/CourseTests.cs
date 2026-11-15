using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class CourseTests : ApiTestBase
{
    [Fact]
    public async Task Create_AsUser_Returns201WithNormalizedCodeAndLocation()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        var user = await CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/courses",
            new { name = " Veri Yapıları ", code = " blg202 ", universityId = university.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var course = (await ReadAsync<CourseResponse>(response))!;
        Assert.Equal("BLG202", course.Code);
        Assert.Equal("Veri Yapıları", course.Name);
        Assert.Equal(university.Id, course.UniversityId);
        Assert.Equal(university.Name, course.UniversityName);
        Assert.EndsWith($"/api/courses/{course.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_WithDuplicateCodeInSameUniversity_Returns409()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        await CreateCourseAsync(admin.Client, university.Id, "BLG101");

        var response = await admin.Client.PostAsJsonAsync(
            "/api/courses",
            new { name = "Başka Ders", code = " blg101 ", universityId = university.Id });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_WithSameCodeInDifferentUniversity_Returns201()
    {
        var admin = await CreateAdminAsync();
        var first = await CreateUniversityAsync(admin.Client);
        var second = await CreateUniversityAsync(admin.Client);
        await CreateCourseAsync(admin.Client, first.Id, "BLG101");

        var response = await admin.Client.PostAsJsonAsync(
            "/api/courses",
            new { name = "Programlama", code = "BLG101", universityId = second.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingUniversity_Returns400()
    {
        var user = await CreateUserAsync();

        var response = await user.Client.PostAsJsonAsync(
            "/api/courses",
            new { name = "Veri Yapıları", code = "BLG202", universityId = 999999 });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_FilteredByUniversity_ReturnsOnlyThatUniversitysCourses()
    {
        var admin = await CreateAdminAsync();
        var first = await CreateUniversityAsync(admin.Client);
        var second = await CreateUniversityAsync(admin.Client);
        var firstCourse = await CreateCourseAsync(admin.Client, first.Id);
        var secondCourse = await CreateCourseAsync(admin.Client, second.Id);

        var all = await admin.Client.GetFromJsonAsync<List<CourseResponse>>("/api/courses", JsonOptions);
        var filtered = await admin.Client.GetFromJsonAsync<List<CourseResponse>>(
            $"/api/courses?universityId={first.Id}", JsonOptions);

        Assert.Equal(2, all!.Count);
        Assert.Equal([firstCourse.Id], filtered!.Select(course => course.Id));
        Assert.DoesNotContain(filtered!, course => course.Id == secondCourse.Id);
    }

    [Fact]
    public async Task Update_IgnoresUniversityIdAndKeepsOriginalUniversity()
    {
        var admin = await CreateAdminAsync();
        var original = await CreateUniversityAsync(admin.Client);
        var other = await CreateUniversityAsync(admin.Client);
        var course = await CreateCourseAsync(admin.Client, original.Id, "BLG101");

        var response = await admin.Client.PutAsJsonAsync(
            $"/api/courses/{course.Id}",
            new { name = "Güncel Ders", code = " blg102 ", universityId = other.Id });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await admin.Client.GetFromJsonAsync<CourseResponse>($"/api/courses/{course.Id}", JsonOptions);
        Assert.Equal(original.Id, updated!.UniversityId);
        Assert.Equal("Güncel Ders", updated.Name);
        Assert.Equal("BLG102", updated.Code);
    }

    [Fact]
    public async Task Update_ToExistingCodeInSameUniversity_Returns409()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        await CreateCourseAsync(admin.Client, university.Id, "BLG101");
        var second = await CreateCourseAsync(admin.Client, university.Id, "BLG102");

        var response = await admin.Client.PutAsJsonAsync(
            $"/api/courses/{second.Id}",
            new { name = "Ders", code = "blg101" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAndUpdate_Missing_Return404()
    {
        var user = await CreateUserAsync();

        await AssertProblemAsync(await user.Client.GetAsync("/api/courses/999999"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await user.Client.PutAsJsonAsync("/api/courses/999999", new { name = "Ders", code = "X1" }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_IsNotSupported()
    {
        var admin = await CreateAdminAsync();
        var (_, course) = await CreateCatalogAsync(admin);

        var response = await admin.Client.DeleteAsync($"/api/courses/{course.Id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
