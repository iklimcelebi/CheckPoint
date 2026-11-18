using System.Net;
using System.Net.Http.Json;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CheckPoint.Api.Tests;

public sealed class ValidationTests : ApiTestBase
{
    private const string InvalidValue = "Değer geçersiz veya hatalı biçimde.";

    public static TheoryData<string, string> InvalidRegistrations => new()
    {
        { """{"username":null,"email":"a@test.local","password":"Parola123"}""", "Kullanıcı adı zorunludur." },
        { """{"username":"   ","email":"a@test.local","password":"Parola123"}""", "Kullanıcı adı zorunludur." },
        { """{"username":"ab","email":"a@test.local","password":"Parola123"}""", "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır." },
        { $$"""{"username":"{{new string('a', 51)}}","email":"a@test.local","password":"Parola123"}""", "Kullanıcı adı 3 ile 50 karakter arasında olmalıdır." },
        { """{"username":" ayse","email":"a@test.local","password":"Parola123"}""", "Kullanıcı adının başında veya sonunda boşluk olamaz." },
        { """{"username":"ayse","email":"gecersiz","password":"Parola123"}""", "Geçerli bir e-posta adresi girin." },
        { """{"username":"ayse","email":"","password":"Parola123"}""", "E-posta adresi zorunludur." },
        { $$"""{"username":"ayse","email":"{{new string('a', 250)}}@test.local","password":"Parola123"}""", "E-posta adresi en fazla 256 karakter olabilir." },
        { """{"username":"ayse","email":"a@test.local","password":"Parol12"}""", "Parola 8 ile 64 karakter arasında olmalıdır." },
        { $$"""{"username":"ayse","email":"a@test.local","password":"{{new string('a', 64)}}1"}""", "Parola 8 ile 64 karakter arasında olmalıdır." },
        { """{"username":"ayse","email":"a@test.local","password":"sadeceharf"}""", "Parola en az bir harf ve bir rakam içermelidir." },
        { """{"username":"ayse","email":"a@test.local","password":"12345678"}""", "Parola en az bir harf ve bir rakam içermelidir." },
        { """{"username":"ayse","email":"a@test.local","password":"!@#$%^&*()"}""", "Parola en az bir harf ve bir rakam içermelidir." },
        { $$"""{"username":"ayse","email":"a@test.local","password":"{{new string('ğ', 36)}}1"}""", "Parola çok uzun: ğ, ü, ş gibi Türkçe karakterler 2 bayt sayıldığından parola en fazla 72 bayt olabilir." },
        { """{"username":"ayse","email":"a@test.local"}""", InvalidValue },
        { """{"username":"ayse",""", InvalidValue }
    };

    [Theory]
    [MemberData(nameof(InvalidRegistrations))]
    public async Task Register_InvalidInput_Returns400WithTurkishMessage(string json, string expectedMessage)
    {
        var response = await PostJsonTextAsync(CreateClient(), "/api/auth/register", json);

        await AssertValidationErrorAsync(response, expectedMessage);
    }

    [Theory]
    [InlineData("Parola12")]
    [InlineData("Şifreğüş1")]
    [InlineData("çok gizli 2024 parolam")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1")]
    [InlineData("ğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğğ1")]
    public async Task Register_PasswordMeetingPolicy_Returns201AndCanLogin(string password)
    {
        var response = await RegisterRawAsync(new { username = "kuralauyan", email = "kural@test.local", password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await LoginAsync("kural@test.local", password);
    }

    [Fact]
    public async Task Login_DoesNotApplyPasswordPolicy()
    {
        var response = await LoginRawAsync("yok@test.local", "kural-disi");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyBody_Returns400WithTurkishMessage()
    {
        var response = await PostJsonTextAsync(CreateClient(), "/api/auth/register", "");

        await AssertValidationErrorAsync(response, "İstek gövdesi zorunludur.");
    }

    [Theory]
    [InlineData("""{"email":"   ","password":"x"}""", "E-posta adresi zorunludur.")]
    [InlineData("""{"email":"a@test.local","password":""}""", "Parola zorunludur.")]
    public async Task Login_InvalidInput_Returns400WithTurkishMessage(string json, string expectedMessage)
    {
        var response = await PostJsonTextAsync(CreateClient(), "/api/auth/login", json);

        await AssertValidationErrorAsync(response, expectedMessage);
    }

    public static TheoryData<string, string> InvalidUniversities => new()
    {
        { """{"name":null,"city":"Denizli"}""", "Üniversite adı zorunludur." },
        { """{"name":"   ","city":"Denizli"}""", "Üniversite adı zorunludur." },
        { """{"name":"  a  ","city":"Denizli"}""", "Üniversite adı 2 ile 200 karakter arasında olmalıdır." },
        { $$"""{"name":"{{new string('a', 201)}}","city":"Denizli"}""", "Üniversite adı 2 ile 200 karakter arasında olmalıdır." },
        { """{"name":"Ege Üniversitesi"}""", "Şehir zorunludur." },
        { $$"""{"name":"Ege Üniversitesi","city":"{{new string('a', 101)}}"}""", "Şehir 2 ile 100 karakter arasında olmalıdır." }
    };

    [Theory]
    [MemberData(nameof(InvalidUniversities))]
    public async Task University_InvalidInput_Returns400WithTurkishMessage(string json, string expectedMessage)
    {
        var admin = await CreateAdminAsync();

        var response = await PostJsonTextAsync(admin.Client, "/api/universities", json);

        await AssertValidationErrorAsync(response, expectedMessage);
    }

    [Fact]
    public async Task University_NameWithSurroundingWhitespaceWithinLimit_IsAcceptedAndTrimmed()
    {
        var admin = await CreateAdminAsync();
        var name = $"  {new string('a', 200)}  ";

        var response = await admin.Client.PostAsJsonAsync("/api/universities", new { name, city = "Denizli" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new string('a', 200), (await ReadAsync<Dtos.UniversityResponse>(response))!.Name);
    }

    public static TheoryData<string, string> InvalidCourses => new()
    {
        { """{"name":"   ","code":"BLG1","universityId":1}""", "Ders adı zorunludur." },
        { """{"name":" x ","code":"BLG1","universityId":1}""", "Ders adı 2 ile 200 karakter arasında olmalıdır." },
        { """{"name":"Ders","code":"   ","universityId":1}""", "Ders kodu zorunludur." },
        { $$"""{"name":"Ders","code":"{{new string('A', 21)}}","universityId":1}""", "Ders kodu 1 ile 20 karakter arasında olmalıdır." },
        { """{"name":"Ders","code":"BLG1","universityId":0}""", "Geçerli bir üniversite seçin." },
        { """{"name":"Ders","code":"BLG1","universityId":"bir"}""", InvalidValue },
        { """{"name":"Ders","universityId":1}""", InvalidValue }
    };

    [Theory]
    [MemberData(nameof(InvalidCourses))]
    public async Task Course_InvalidInput_Returns400WithTurkishMessage(string json, string expectedMessage)
    {
        var user = await CreateUserAsync();

        var response = await PostJsonTextAsync(user.Client, "/api/courses", json);

        await AssertValidationErrorAsync(response, expectedMessage);
    }

    [Theory]
    [InlineData("""{"title":"   ","type":"LectureNote"}""", "Başlık zorunludur.")]
    [InlineData("""{"title":"Başlık"}""", "Kaynak türü geçersiz.")]
    [InlineData("""{"title":"Başlık","type":"Bilinmeyen"}""", InvalidValue)]
    [InlineData("""{"title":"Başlık","type":99}""", "Kaynak türü geçersiz.")]
    public async Task ResourceUpdate_InvalidInput_Returns400WithTurkishMessage(string json, string expectedMessage)
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);

        var response = await admin.Client.PutAsync(
            $"/api/resources/{resource.Id}",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await AssertValidationErrorAsync(response, expectedMessage);
    }

    [Fact]
    public async Task ResourceUpdate_DescriptionTooLong_Returns400()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);

        var response = await admin.Client.PutAsJsonAsync(
            $"/api/resources/{resource.Id}",
            new { title = "Başlık", description = new string('a', 2001), type = "LectureNote" });

        await AssertValidationErrorAsync(response, "Açıklama en fazla 2000 karakter olabilir.");
    }

    [Fact]
    public async Task ResourceUpload_InvalidFormFields_Return400WithTurkishMessages()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        await AssertValidationErrorAsync(
            await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text(), title: "   "),
            "Başlık zorunludur.");
        await AssertValidationErrorAsync(
            await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text(), title: new string('a', 201)),
            "Başlık en fazla 200 karakter olabilir.");
        await AssertValidationErrorAsync(
            await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text(),
                description: new string('a', 2001)),
            "Açıklama en fazla 2000 karakter olabilir.");
        await AssertValidationErrorAsync(
            await UploadRawAsync(admin.Client, 0, course.Id, TestFiles.Text()),
            "Geçerli bir üniversite seçin.");
        await AssertValidationErrorAsync(
            await UploadRawAsync(admin.Client, university.Id, course.Id, file: null),
            "Yüklenecek dosya zorunludur.");
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task ResourceUpload_AsJson_Returns415WithTurkishTitle()
    {
        var user = await CreateUserAsync();

        var response = await PostJsonTextAsync(user.Client, "/api/resources", """{"title":"x"}""");

        var problem = await AssertProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
        Assert.Equal("Desteklenmeyen içerik türü", problem.Title);
    }

    [Fact]
    public async Task UnknownRouteAndWrongMethod_ReturnTurkishProblemDetails()
    {
        var user = await CreateUserAsync();

        var notFound = await user.Client.GetAsync("/api/olmayan-adres");
        var methodNotAllowed = await user.Client.DeleteAsync("/api/resources/1/view");

        Assert.Equal("Bulunamadı", (await AssertProblemAsync(notFound, HttpStatusCode.NotFound)).Title);
        Assert.Equal(
            "Yönteme izin verilmiyor",
            (await AssertProblemAsync(methodNotAllowed, HttpStatusCode.MethodNotAllowed)).Title);
    }

    [Fact]
    public async Task ProblemResponses_UseTurkishTitles()
    {
        var admin = await CreateAdminAsync();
        await CreateUniversityAsync(admin.Client, "Ege Üniversitesi");

        var notFound = await admin.Client.GetAsync("/api/universities/999999");
        var conflict = await admin.Client.PostAsJsonAsync(
            "/api/universities", new { name = "Ege Üniversitesi", city = "İzmir" });

        var notFoundProblem = await ReadAsync<ProblemDetails>(notFound);
        var conflictProblem = await ReadAsync<ProblemDetails>(conflict);
        Assert.Equal("Bulunamadı", notFoundProblem!.Title);
        Assert.Equal("Üniversite bulunamadı.", notFoundProblem.Detail);
        Assert.Equal("Çakışma", conflictProblem!.Title);
    }
}
