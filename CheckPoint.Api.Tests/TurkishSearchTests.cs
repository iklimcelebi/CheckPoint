using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class TurkishSearchTests : ApiTestBase
{
    [Theory]
    [InlineData("izmir")]
    [InlineData("İZMİR")]
    [InlineData("ege universitesi")]
    [InlineData("EGE ÜNİVERSİTESİ")]
    public async Task Universities_SearchIgnoresTurkishCaseAndDiacritics(string q)
    {
        var admin = await CreateAdminAsync();
        await admin.Client.PostAsJsonAsync("/api/universities", new { name = "Ege Üniversitesi", city = "İzmir" });
        await admin.Client.PostAsJsonAsync("/api/universities", new { name = "Pamukkale Üniversitesi", city = "Denizli" });

        var result = await admin.Client.GetFromJsonAsync<PagedResponse<UniversityResponse>>(
            $"/api/universities?q={Uri.EscapeDataString(q)}", JsonOptions);

        Assert.Equal("Ege Üniversitesi", Assert.Single(result!.Items).Name);
    }

    [Theory]
    [InlineData("bil105")]
    [InlineData("programlamaya giris")]
    [InlineData("GİRİŞ")]
    public async Task Courses_SearchMatchesAsciiCodesAndTurkishNames(string q)
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);
        await admin.Client.PostAsJsonAsync("/api/courses",
            new { name = "Programlamaya Giriş", code = "BIL105", universityId = university.Id });
        await admin.Client.PostAsJsonAsync("/api/courses",
            new { name = "Kalkülüs", code = "MAT101", universityId = university.Id });

        var result = await admin.Client.GetFromJsonAsync<PagedResponse<CourseResponse>>(
            $"/api/courses?q={Uri.EscapeDataString(q)}", JsonOptions);

        Assert.Equal("BIL105", Assert.Single(result!.Items).Code);
    }

    [Theory]
    [InlineData("calisma")]
    [InlineData("ÇALIŞMA")]
    [InlineData("istatistik")]
    public async Task Resources_SearchIgnoresTurkishCaseAndDiacritics(string q)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        await UploadAsync(admin.Client, university.Id, course.Id, title: "Çalışma soruları",
            description: "İstatistik konuları");
        await UploadAsync(admin.Client, university.Id, course.Id, title: "Fizik notları");

        var result = await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            $"/api/resources?q={Uri.EscapeDataString(q)}", JsonOptions);

        Assert.Equal("Çalışma soruları", Assert.Single(result!.Items).Title);
    }
}
