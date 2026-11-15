using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class CatalogPagingTests : ApiTestBase
{
    [Fact]
    public async Task Universities_WithoutParameters_ReturnDefaultPage()
    {
        var admin = await CreateAdminAsync();
        await CreateUniversityAsync(admin.Client, "Ege Üniversitesi");

        var result = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            "/api/universities", JsonOptions);

        Assert.Equal(1, result!.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Ege Üniversitesi", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task Universities_PaginateAndSearchByNameOrCity()
    {
        var admin = await CreateAdminAsync();
        foreach (var name in new[] { "Akdeniz Üniversitesi", "Bursa Teknik Üniversitesi", "Celal Bayar Üniversitesi" })
            await CreateUniversityAsync(admin.Client, name);
        await admin.Client.PostAsJsonAsync("/api/universities", new { name = "Dokuz Eylül Üniversitesi", city = "İzmir" });

        var page2 = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            "/api/universities?page=2&pageSize=2", JsonOptions);
        var byName = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            "/api/universities?q=%20teknik%20", JsonOptions);
        var byCity = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            "/api/universities?q=İzmir", JsonOptions);

        Assert.Equal(4, page2!.TotalCount);
        Assert.Equal(["Celal Bayar Üniversitesi", "Dokuz Eylül Üniversitesi"], page2.Items.Select(u => u.Name));
        Assert.Equal("Bursa Teknik Üniversitesi", Assert.Single(byName!.Items).Name);
        Assert.Equal("Dokuz Eylül Üniversitesi", Assert.Single(byCity!.Items).Name);
    }

    [Fact]
    public async Task Courses_PaginateFilterAndSearchByNameOrCode()
    {
        var admin = await CreateAdminAsync();
        var first = await CreateUniversityAsync(admin.Client);
        var second = await CreateUniversityAsync(admin.Client);
        await admin.Client.PostAsJsonAsync("/api/courses", new { name = "Algoritmalar", code = "BLG301", universityId = first.Id });
        await admin.Client.PostAsJsonAsync("/api/courses", new { name = "Fizik", code = "FIZ101", universityId = first.Id });
        await admin.Client.PostAsJsonAsync("/api/courses", new { name = "Matematik", code = "MAT101", universityId = first.Id });
        await admin.Client.PostAsJsonAsync("/api/courses", new { name = "Algoritmalar", code = "BLG301", universityId = second.Id });

        var page = await admin.Client.GetFromJsonAsync<PagedResponse<CourseResponse>>(
            $"/api/courses?universityId={first.Id}&page=1&pageSize=2", JsonOptions);
        var byCode = await admin.Client.GetFromJsonAsync<PagedResponse<CourseResponse>>(
            "/api/courses?q=101", JsonOptions);
        var byNameInUniversity = await admin.Client.GetFromJsonAsync<PagedResponse<CourseResponse>>(
            $"/api/courses?q=algoritma&universityId={second.Id}", JsonOptions);

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(["Algoritmalar", "Fizik"], page.Items.Select(c => c.Name));
        Assert.Equal(["Fizik", "Matematik"], byCode!.Items.Select(c => c.Name));
        Assert.Equal(second.Id, Assert.Single(byNameInUniversity!.Items).UniversityId);
    }

    [Theory]
    [InlineData("/api/universities?page=0")]
    [InlineData("/api/universities?pageSize=101")]
    [InlineData("/api/courses?page=0")]
    [InlineData("/api/courses?pageSize=0")]
    public async Task InvalidPaging_Returns400(string url)
    {
        var user = await CreateUserAsync();

        await AssertProblemAsync(await user.Client.GetAsync(url), HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/universities")]
    [InlineData("/api/courses")]
    [InlineData("/api/resources")]
    public async Task SearchTextTooLong_Returns400WithTurkishMessage(string url)
    {
        var user = await CreateUserAsync();

        var response = await user.Client.GetAsync($"{url}?q={new string('a', 101)}");

        await AssertValidationErrorAsync(response, "Arama metni en fazla 100 karakter olabilir.");
    }

    [Theory]
    [InlineData("/api/universities?page=abc")]
    [InlineData("/api/courses?universityId=abc")]
    [InlineData("/api/resources?type=Bilinmeyen")]
    public async Task InvalidQueryValues_Return400WithTurkishMessage(string url)
    {
        var user = await CreateUserAsync();

        await AssertValidationErrorAsync(await user.Client.GetAsync(url), "Değer geçersiz veya hatalı biçimde.");
    }
}
