using System.Net;
using System.Text;
using CheckPoint.Api.Models;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Tests;

public sealed class ResourceUploadTests : ApiTestBase
{
    private const int MaxFileSize = 20 * 1024 * 1024;

    [Fact]
    public async Task Upload_ValidFile_Returns201AndStoresFileUnderGeneratedName()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var user = await CreateUserAsync();

        var response = await UploadRawAsync(
            user.Client, university.Id, course.Id, TestFiles.Pdf("../../vize notları.pdf"),
            title: " Vize Notları ", type: "PastExamQuestion", description: " Açıklama ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = (await ReadAsync<Dtos.ResourceResponse>(response))!;
        Assert.Equal($"/api/resources/{resource.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Vize Notları", resource.Title);
        Assert.Equal("Açıklama", resource.Description);
        Assert.Equal(ResourceType.PastExamQuestion, resource.Type);
        Assert.Equal("vize notları.pdf", resource.OriginalFileName);
        Assert.Equal(university.Id, resource.University.Id);
        Assert.Equal(course.Id, resource.Course.Id);
        Assert.Equal(user.Id, resource.UploadedBy.Id);
        Assert.Equal(user.Username, resource.UploadedBy.Username);

        var stored = Assert.Single(StoredFiles());
        Assert.Matches("^[0-9a-f]{32}\\.pdf$", Path.GetFileName(stored));

        await using var db = Factory.CreateDbContext();
        var entity = await db.EducationalResources.SingleAsync();
        Assert.Equal("application/pdf", entity.ContentType);
        Assert.Equal(Path.GetFileName(stored), entity.StoredFileName);
    }

    [Theory]
    [InlineData("program.exe", "application/octet-stream")]
    [InlineData("notlar.pdf", "text/plain")]
    [InlineData("notlar.txt", "application/pdf")]
    [InlineData("dosya", "text/plain")]
    public async Task Upload_UnsupportedExtensionOrContentType_Returns415(string fileName, string contentType)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(
            admin.Client, university.Id, course.Id,
            new TestFile(fileName, contentType, Encoding.UTF8.GetBytes("içerik")));

        await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
        Assert.Empty(StoredFiles());
    }

    [Theory]
    [InlineData("sahte.pdf", "application/pdf")]
    [InlineData("sahte.png", "image/png")]
    [InlineData("sahte.jpg", "image/jpeg")]
    [InlineData("sahte.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("sahte.pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public async Task Upload_FakeExtensionWithWrongMagicBytes_Returns415(string fileName, string contentType)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(
            admin.Client, university.Id, course.Id,
            new TestFile(fileName, contentType, Encoding.UTF8.GetBytes("MZ bu aslında bir metin dosyası")));

        await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_FileAtExactLimit_Returns201()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.OfSize(MaxFileSize));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Upload_FileOverLimit_Returns413()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.OfSize(MaxFileSize + 1));

        await AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(
            admin.Client, university.Id, course.Id, new TestFile("bos.txt", "text/plain", []));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_WithoutFile_Returns400()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, file: null);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_CourseFromAnotherUniversity_Returns400()
    {
        var admin = await CreateAdminAsync();
        var (_, course) = await CreateCatalogAsync(admin);
        var otherUniversity = await CreateUniversityAsync(admin.Client);

        var response = await UploadRawAsync(admin.Client, otherUniversity.Id, course.Id, TestFiles.Text());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_MissingCourse_Returns400()
    {
        var admin = await CreateAdminAsync();
        var university = await CreateUniversityAsync(admin.Client);

        var response = await UploadRawAsync(admin.Client, university.Id, 999999, TestFiles.Text());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Bilinmeyen")]
    [InlineData("99")]
    public async Task Upload_InvalidResourceType_Returns400(string type)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text(), type: type);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        var response = await UploadRawAsync(CreateClient(), 1, 1, TestFiles.Text());

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }
}
