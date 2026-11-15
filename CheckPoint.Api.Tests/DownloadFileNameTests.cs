using System.Net;
using CheckPoint.Api.Controllers;
using CheckPoint.Api.Tests.Infrastructure;

namespace CheckPoint.Api.Tests;

public sealed class DownloadFileNameTests : ApiTestBase
{
    [Theory]
    [InlineData("Çalışma Soruları ğüşıöç İĞÜŞÖÇ.pdf", "Çalışma Soruları ğüşıöç İĞÜŞÖÇ.pdf")]
    [InlineData("rapor; v2 (final) 'son'.pdf", "rapor; v2 (final) 'son'.pdf")]
    [InlineData("..\\..\\windows\\gizli.pdf", "gizli.pdf")]
    [InlineData("../../etc/gizli.pdf", "gizli.pdf")]
    [InlineData("a<b>c|d?e*f:g.pdf", "a_b_c_d_e_f_g.pdf")]
    [InlineData("100% doğru & tam.pdf", "100% doğru & tam.pdf")]
    public async Task Download_EncodesOriginalFileNameSafely(string uploadedName, string expectedName)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id, TestFiles.Pdf(uploadedName));
        Assert.Equal(expectedName, resource.OriginalFileName);

        var response = await admin.Client.GetAsync($"/api/resources/{resource.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rawHeader = Assert.Single(response.Content.Headers.GetValues("Content-Disposition"));
        Assert.All(rawHeader, character => Assert.InRange(character, (char)0x20, (char)0x7E));
        var disposition = response.Content.Headers.ContentDisposition!;
        Assert.Equal("attachment", disposition.DispositionType);
        Assert.Equal(expectedName, disposition.FileNameStar);
        Assert.False(string.IsNullOrWhiteSpace(disposition.FileName));
    }

    [Theory]
    [InlineData("   .pdf")]
    [InlineData(" . .pdf")]
    [InlineData("....pdf")]
    public async Task Upload_FileNameWithoutBaseName_Returns400(string fileName)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Pdf(fileName));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Empty(StoredFiles());
    }

    [Theory]
    [InlineData("not\tlar.pdf", "notlar.pdf")]
    [InlineData("not\r\nlar.pdf", "notlar.pdf")]
    [InlineData("fatura‮fdp.exe.pdf", "faturafdp.exe.pdf")]
    [InlineData("  notlar.pdf  ", "notlar.pdf")]
    [InlineData("rapor \"final\".pdf", "rapor _final_.pdf")]
    [InlineData("notlar.pdf. . ", "notlar.pdf")]
    [InlineData("klasör/alt\\ödev.docx", "ödev.docx")]
    public void SanitizeFileName_RemovesControlAndFormatCharacters(string input, string expected)
    {
        Assert.Equal(expected, ResourcesController.SanitizeFileName(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("klasör/")]
    [InlineData(".pdf")]
    public void SanitizeFileName_ReturnsNullForUnusableNames(string? input)
    {
        Assert.Null(ResourcesController.SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_RejectsNamesLongerThan260Characters()
    {
        Assert.Null(ResourcesController.SanitizeFileName($"{new string('a', 257)}.pdf"));
        Assert.Equal(260, ResourcesController.SanitizeFileName($"{new string('a', 256)}.pdf")!.Length);
    }
}
