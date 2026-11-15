using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CheckPoint.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CheckPoint.Api.Tests.Infrastructure;

public abstract class ApiTestBase : IAsyncLifetime
{
    public const string DefaultPassword = "Parola123";

    protected static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private int _sequence;

    protected CheckPointApiFactory Factory { get; } = new();

    public async Task InitializeAsync() => await Factory.CreateDatabaseAsync();

    public async Task DisposeAsync() => await Factory.DisposeAsync();

    protected HttpClient CreateClient(string? token = null)
    {
        var client = Factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    protected string NextName(string prefix) => $"{prefix}{Interlocked.Increment(ref _sequence)}";

    protected async Task<HttpResponseMessage> RegisterRawAsync(object body) =>
        await CreateClient().PostAsJsonAsync("/api/auth/register", body);

    protected async Task<HttpResponseMessage> LoginRawAsync(string email, string password) =>
        await CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

    protected async Task<TestUser> CreateUserAsync(string? username = null)
    {
        username ??= NextName("user");
        var email = $"{username}@test.local";
        var response = await RegisterRawAsync(new { username, email, password = DefaultPassword });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var login = await LoginAsync(email, DefaultPassword);
        return new TestUser(login.User.Id, login.User.Username, email, CreateClient(login.Token));
    }

    protected async Task<TestUser> CreateAdminAsync()
    {
        var login = await LoginAsync(Factory.AdminEmail, Factory.AdminPassword);
        Assert.Equal("Admin", login.User.Role);
        return new TestUser(login.User.Id, login.User.Username, Factory.AdminEmail, CreateClient(login.Token));
    }

    protected async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await LoginRawAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAsync<LoginResponse>(response))!;
    }

    protected async Task<UniversityResponse> CreateUniversityAsync(HttpClient adminClient, string? name = null)
    {
        var response = await adminClient.PostAsJsonAsync(
            "/api/universities",
            new { name = name ?? NextName("Üniversite "), city = "Denizli" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<UniversityResponse>(response))!;
    }

    protected async Task<CourseResponse> CreateCourseAsync(HttpClient client, int universityId, string? code = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/courses",
            new { name = "Veri Yapıları", code = code ?? NextName("BLG"), universityId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<CourseResponse>(response))!;
    }

    protected async Task<(UniversityResponse University, CourseResponse Course)> CreateCatalogAsync(TestUser admin)
    {
        var university = await CreateUniversityAsync(admin.Client);
        var course = await CreateCourseAsync(admin.Client, university.Id);
        return (university, course);
    }

    protected static async Task<HttpResponseMessage> UploadRawAsync(
        HttpClient client,
        int universityId,
        int courseId,
        TestFile? file,
        string title = "Ders notları",
        string type = "LectureNote",
        string? description = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(title), "Title");
        if (description is not null)
            form.Add(new StringContent(description), "Description");
        form.Add(new StringContent(universityId.ToString()), "UniversityId");
        form.Add(new StringContent(courseId.ToString()), "CourseId");
        form.Add(new StringContent(type), "Type");

        if (file is not null)
        {
            var fileContent = new ByteArrayContent(file.Content);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
            form.Add(fileContent, "File", file.FileName);
        }

        return await client.PostAsync("/api/resources", form);
    }

    protected static async Task<ResourceResponse> UploadAsync(
        HttpClient client,
        int universityId,
        int courseId,
        TestFile? file = null,
        string title = "Ders notları",
        string type = "LectureNote",
        string? description = null)
    {
        var response = await UploadRawAsync(
            client, universityId, courseId, file ?? TestFiles.Text(), title, type, description);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadAsync<ResourceResponse>(response))!;
    }

    protected static async Task<T?> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions);

    protected static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadAsync<ProblemDetails>(response);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        return problem;
    }

    protected static async Task AssertValidationErrorAsync(HttpResponseMessage response, string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.NotNull(problem);
        Assert.Equal(Api.Extensions.ApiBehaviorExtensions.ValidationTitle, problem.Title);
        var messages = problem.Errors.SelectMany(error => error.Value).ToArray();
        Assert.Contains(expectedMessage, messages);
        Assert.DoesNotContain(messages, message => message.StartsWith("The ", StringComparison.Ordinal));
    }

    protected static async Task<HttpResponseMessage> PostJsonTextAsync(HttpClient client, string url, string json) =>
        await client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

    protected string[] StoredFiles() =>
        Directory.Exists(Factory.UploadRoot) ? Directory.GetFiles(Factory.UploadRoot) : [];

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed record TestUser(int Id, string Username, string Email, HttpClient Client);
