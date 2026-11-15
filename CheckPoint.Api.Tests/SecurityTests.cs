using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CheckPoint.Api.Services;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CheckPoint.Api.Tests;

public sealed class SecurityTests : ApiTestBase
{
    private const string AllowedOrigin = "http://localhost:5173";

    [Fact]
    public async Task AuthEndpoints_AfterPermitLimit_Return429WithRetryAfter()
    {
        Factory.Settings["RateLimiting:Auth:PermitLimit"] = "3";
        Factory.Settings["RateLimiting:Auth:WindowSeconds"] = "60";

        var register = await RegisterRawAsync(new { username = "hizli", email = "hizli@test.local", password = DefaultPassword });
        var wrongPassword = await LoginRawAsync("hizli@test.local", "YanlisParola1");
        var login = await LoginRawAsync("hizli@test.local", DefaultPassword);
        var blocked = await LoginRawAsync("hizli@test.local", DefaultPassword);
        var blockedRegister = await RegisterRawAsync(new { username = "diger", email = "diger@test.local", password = DefaultPassword });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var problem = await AssertProblemAsync(blocked, HttpStatusCode.TooManyRequests);
        Assert.Equal("Çok fazla istek", problem.Title);
        Assert.Equal("Çok fazla deneme yaptınız. Lütfen biraz bekleyip tekrar deneyin.", problem.Detail);
        Assert.True(int.Parse(Assert.Single(blocked.Headers.GetValues("Retry-After"))) > 0);
        await AssertProblemAsync(blockedRegister, HttpStatusCode.TooManyRequests);

        var token = (await ReadAsync<Dtos.LoginResponse>(login))!.Token;
        var otherEndpoint = await CreateClient(token).GetAsync("/api/universities");
        Assert.Equal(HttpStatusCode.OK, otherEndpoint.StatusCode);
    }

    [Theory]
    [InlineData("/api/universities", true, HttpStatusCode.OK)]
    [InlineData("/api/universities", false, HttpStatusCode.Unauthorized)]
    [InlineData("/api/olmayan-adres", true, HttpStatusCode.NotFound)]
    public async Task Responses_IncludeSecurityHeaders(string url, bool authenticated, HttpStatusCode expectedStatus)
    {
        var client = authenticated ? (await CreateUserAsync()).Client : CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(expectedStatus, response.StatusCode);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Download_IncludesSecurityHeadersAndStoredContentType()
    {
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id, TestFiles.Png());

        var response = await admin.Client.GetAsync($"/api/resources/{resource.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        AssertSecurityHeaders(response);
        Assert.Single(response.Headers.GetValues("X-Content-Type-Options"));
    }

    [Fact]
    public async Task UnhandledException_Returns500WithoutDetailsAndWithSecurityHeaders()
    {
        Factory.ConfigureServices = services =>
            services.AddSingleton<IFileScanner>(new FakeFileScanner(() => throw new InvalidOperationException("gizli-iç-ayrıntı")));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = System.Text.Json.JsonSerializer.Deserialize<Microsoft.AspNetCore.Mvc.ProblemDetails>(body, JsonOptions)!;
        Assert.Equal("İstek işlenirken beklenmeyen bir hata oluştu.", problem.Detail);
        Assert.DoesNotContain("gizli", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        AssertSecurityHeaders(response);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Cors_AllowsOnlyConfiguredOrigin()
    {
        Factory.Settings["Cors:AllowedOrigins:0"] = $"{AllowedOrigin}/";
        var client = CreateClient();

        var allowed = await client.SendAsync(Preflight(AllowedOrigin));
        var denied = await client.SendAsync(Preflight("http://kotu.example"));

        Assert.Equal(AllowedOrigin, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("authorization", string.Join(",", allowed.Headers.GetValues("Access-Control-Allow-Headers")),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));

        var user = await CreateUserAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/universities");
        request.Headers.Add("Origin", AllowedOrigin);
        var actual = await user.Client.SendAsync(request);
        Assert.Equal(AllowedOrigin, Assert.Single(actual.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("Content-Disposition", string.Join(",", actual.Headers.GetValues("Access-Control-Expose-Headers")));
    }

    [Fact]
    public async Task Cors_IsClosedWhenNoOriginsConfigured()
    {
        var response = await CreateClient().SendAsync(Preflight(AllowedOrigin));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("localhost:5173")]
    [InlineData("http://localhost:5173/yol")]
    public void Cors_InvalidOriginConfiguration_StopsStartup(string origin)
    {
        Factory.Settings["Cors:AllowedOrigins:0"] = origin;

        var exception = Assert.ThrowsAny<Exception>(() => Factory.CreateClient());

        Assert.Contains("Cors:AllowedOrigins", exception.GetBaseException().Message);
    }

    [Fact]
    public async Task HttpsRedirection_RedirectsHttpWhenHttpsPortIsConfigured()
    {
        Factory.Settings["https_port"] = "7237";
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/universities");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https://localhost:7237/api/universities", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Upload_OverConfiguredLimitWithContentLength_Returns413BeforeReadingForm()
    {
        Factory.Settings["FileStorage:MaxFileSizeMB"] = "1";
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var slightlyLarger = await UploadRawAsync(
            admin.Client, university.Id, course.Id, TestFiles.OfSize(1024 * 1024 + 1));
        var farLarger = await UploadRawAsync(
            admin.Client, university.Id, course.Id, TestFiles.OfSize(3 * 1024 * 1024));

        Assert.Equal("Dosya izin verilen boyut sınırını aşıyor.",
            (await AssertProblemAsync(slightlyLarger, HttpStatusCode.RequestEntityTooLarge)).Detail);
        Assert.Equal("Dosya izin verilen boyut sınırını aşıyor.",
            (await AssertProblemAsync(farLarger, HttpStatusCode.RequestEntityTooLarge)).Detail);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_OverConfiguredLimitWithoutContentLength_Returns413()
    {
        Factory.Settings["FileStorage:MaxFileSizeMB"] = "1";
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Büyük dosya"), "Title");
        form.Add(new StringContent(university.Id.ToString()), "UniversityId");
        form.Add(new StringContent(course.Id.ToString()), "CourseId");
        form.Add(new StringContent("LectureNote"), "Type");
        var fileContent = new StreamContent(new NonSeekableStream(new byte[3 * 1024 * 1024]));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "File", "buyuk.txt");

        var response = await admin.Client.PostAsync("/api/resources", form);

        await AssertProblemAsync(response, HttpStatusCode.RequestEntityTooLarge);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_InfectedFile_Returns400AndStoresNothing()
    {
        Factory.ConfigureServices = services =>
            services.AddSingleton<IFileScanner>(new FakeFileScanner(() => FileScanStatus.Infected));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

        Assert.Equal("Dosya güvenlik taramasından geçemedi.",
            (await AssertProblemAsync(response, HttpStatusCode.BadRequest)).Detail);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_ScannerUnavailable_Returns503AndStoresNothing()
    {
        Factory.ConfigureServices = services =>
            services.AddSingleton<IFileScanner>(new FakeFileScanner(() => FileScanStatus.Unavailable));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task Upload_ScannerReceivesFileContent()
    {
        byte[]? scanned = null;
        Factory.ConfigureServices = services =>
            services.AddSingleton<IFileScanner>(new FakeFileScanner(() => FileScanStatus.Clean, bytes => scanned = bytes));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var file = TestFiles.Text(content: "taranacak içerik");

        await UploadAsync(admin.Client, university.Id, course.Id, file);

        Assert.Equal(file.Content, scanned);
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/universities");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        return request;
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("default-src 'none'", Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    private sealed class FakeFileScanner(Func<FileScanStatus> result, Action<byte[]>? capture = null) : IFileScanner
    {
        public async Task<FileScanStatus> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            capture?.Invoke(buffer.ToArray());
            return result();
        }
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
