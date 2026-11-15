using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class ResourceManagementTests : ApiTestBase
{
    [Fact]
    public async Task GetById_ReturnsResource_AndMissingReturns404()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var uploaded = await UploadAsync(admin.Client, university.Id, course.Id);

        var fetched = await admin.Client.GetFromJsonAsync<ResourceResponse>(
            $"/api/resources/{uploaded.Id}", JsonOptions);

        Assert.Equal(uploaded.Id, fetched!.Id);
        Assert.Equal(uploaded.Title, fetched.Title);
        await AssertProblemAsync(await admin.Client.GetAsync("/api/resources/999999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_ReturnsOriginalContentTypeAndAttachmentDisposition()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var file = TestFiles.Pdf("ders-notu.pdf");
        var uploaded = await UploadAsync(admin.Client, university.Id, course.Id, file);
        var user = await CreateUserAsync();

        var response = await user.Client.GetAsync($"/api/resources/{uploaded.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(file.Content, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var disposition = response.Content.Headers.ContentDisposition;
        Assert.Equal("attachment", disposition?.DispositionType);
        Assert.Equal("ders-notu.pdf", disposition?.FileNameStar);
        Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task Download_MissingResourceOrFile_Returns404()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var uploaded = await UploadAsync(admin.Client, university.Id, course.Id);
        File.Delete(Assert.Single(StoredFiles()));

        await AssertProblemAsync(
            await admin.Client.GetAsync("/api/resources/999999/download"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await admin.Client.GetAsync($"/api/resources/{uploaded.Id}/download"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAndDelete_ByNonOwner_Return403AndKeepResource()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var owner = await CreateUserAsync();
        var other = await CreateUserAsync();
        var uploaded = await UploadAsync(owner.Client, university.Id, course.Id);

        var put = await other.Client.PutAsJsonAsync(
            $"/api/resources/{uploaded.Id}",
            new { title = "Ele geçirildi", type = "LectureNote" });
        var delete = await other.Client.DeleteAsync($"/api/resources/{uploaded.Id}");

        await AssertProblemAsync(put, HttpStatusCode.Forbidden);
        await AssertProblemAsync(delete, HttpStatusCode.Forbidden);
        var current = await owner.Client.GetFromJsonAsync<ResourceResponse>(
            $"/api/resources/{uploaded.Id}", JsonOptions);
        Assert.Equal(uploaded.Title, current!.Title);
        Assert.Single(StoredFiles());
    }

    [Fact]
    public async Task Update_ByOwner_Returns204AndChangesOnlyEditableFields()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var owner = await CreateUserAsync();
        var uploaded = await UploadAsync(owner.Client, university.Id, course.Id, description: "Eski");

        var response = await owner.Client.PutAsJsonAsync(
            $"/api/resources/{uploaded.Id}",
            new { title = " Yeni Başlık ", description = "   ", type = "StudyQuestion", courseId = 999 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await owner.Client.GetFromJsonAsync<ResourceResponse>(
            $"/api/resources/{uploaded.Id}", JsonOptions);
        Assert.Equal("Yeni Başlık", updated!.Title);
        Assert.Null(updated.Description);
        Assert.Equal(ResourceType.StudyQuestion, updated.Type);
        Assert.Equal(course.Id, updated.Course.Id);
        Assert.Equal(uploaded.OriginalFileName, updated.OriginalFileName);
    }

    [Fact]
    public async Task UpdateAndDelete_ByAdminOnOthersResource_Return204()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var owner = await CreateUserAsync();
        var uploaded = await UploadAsync(owner.Client, university.Id, course.Id);

        var put = await admin.Client.PutAsJsonAsync(
            $"/api/resources/{uploaded.Id}",
            new { title = "Moderasyon", type = "LectureNote" });
        var delete = await admin.Client.DeleteAsync($"/api/resources/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Delete_ByOwner_Returns204AndRemovesRecordAndFile()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var owner = await CreateUserAsync();
        var uploaded = await UploadAsync(owner.Client, university.Id, course.Id);
        Assert.Single(StoredFiles());

        var response = await owner.Client.DeleteAsync($"/api/resources/{uploaded.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(StoredFiles());
        await AssertProblemAsync(
            await owner.Client.GetAsync($"/api/resources/{uploaded.Id}"), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await owner.Client.DeleteAsync($"/api/resources/{uploaded.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_PaginatesNewestFirst()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var first = await UploadAsync(admin.Client, university.Id, course.Id, title: "Birinci");
        var second = await UploadAsync(admin.Client, university.Id, course.Id, title: "İkinci");
        var third = await UploadAsync(admin.Client, university.Id, course.Id, title: "Üçüncü");

        var page1 = await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/resources?page=1&pageSize=2", JsonOptions);
        var page2 = await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/resources?page=2&pageSize=2", JsonOptions);
        var defaults = await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/resources", JsonOptions);

        Assert.Equal(3, page1!.TotalCount);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Equal([third.Id, second.Id], page1.Items.Select(item => item.Id));
        Assert.Equal([first.Id], page2!.Items.Select(item => item.Id));
        Assert.Equal(1, defaults!.Page);
        Assert.Equal(20, defaults.PageSize);
        Assert.Equal(3, defaults.Items.Count);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task List_InvalidPaging_Returns400(string query)
    {
        var user = await CreateUserAsync();

        await AssertProblemAsync(await user.Client.GetAsync($"/api/resources?{query}"), HttpStatusCode.BadRequest);
        await AssertProblemAsync(
            await user.Client.GetAsync($"/api/users/me/resources?{query}"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_FiltersByUniversityCourseTypeAndSearch()
    {
        var admin = await CreateAdminAsync();
        var (university1, course1) = await CreateCatalogAsync(admin);
        var course1b = await CreateCourseAsync(admin.Client, university1.Id);
        var (university2, course2) = await CreateCatalogAsync(admin);

        var note = await UploadAsync(admin.Client, university1.Id, course1.Id, title: "Algoritma notları");
        var exam = await UploadAsync(
            admin.Client, university1.Id, course1b.Id, title: "Final", type: "PastExamQuestion",
            description: "2024 algoritma finali");
        var other = await UploadAsync(admin.Client, university2.Id, course2.Id, title: "Fizik sunumu",
            type: "Presentation");

        async Task<int[]> Ids(string query) =>
            (await admin.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
                $"/api/resources?{query}", JsonOptions))!.Items.Select(item => item.Id).Order().ToArray();

        Assert.Equal(new[] { note.Id, exam.Id }.Order(), await Ids($"universityId={university1.Id}"));
        Assert.Equal([exam.Id], await Ids($"courseId={course1b.Id}"));
        Assert.Equal([other.Id], await Ids("type=Presentation"));
        Assert.Equal(new[] { note.Id, exam.Id }.Order(), await Ids("q=algoritma"));
        Assert.Equal([exam.Id], await Ids($"q=algoritma&type=PastExamQuestion&universityId={university1.Id}"));
        Assert.Empty(await Ids($"universityId={university2.Id}&type=LectureNote"));
    }

    [Fact]
    public async Task MyResources_ReturnsOnlyCurrentUsersResources()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var first = await CreateUserAsync();
        var second = await CreateUserAsync();
        var mine = await UploadAsync(first.Client, university.Id, course.Id, title: "Benim");
        await UploadAsync(second.Client, university.Id, course.Id, title: "Onun");
        await UploadAsync(admin.Client, university.Id, course.Id, title: "Yöneticinin");

        var result = await first.Client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/users/me/resources", JsonOptions);

        Assert.Equal(1, result!.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(mine.Id, item.Id);
        Assert.Equal(first.Id, item.UploadedBy.Id);
    }
}
