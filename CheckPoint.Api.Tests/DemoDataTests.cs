using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CheckPoint.Api.Data;
using CheckPoint.Api.Dtos;
using CheckPoint.Api.Models;
using CheckPoint.Api.Services;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckPoint.Api.Tests;

public sealed class DemoDataTests : ApiTestBase
{
    [Fact]
    public async Task Seed_RunTwice_DoesNotDuplicateAnything()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        Assert.Equal((3, 7, 2, 7), (first.Universities, first.Courses, first.Users, first.Resources));
        Assert.Equal((0, 0, 0, 0, 0), (second.Universities, second.Courses, second.Users, second.Resources, second.RestoredFiles));
        await using var db = Factory.CreateDbContext();
        Assert.Equal(3, await db.Universities.CountAsync());
        Assert.Equal(7, await db.Courses.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync(user => user.Role == "User"));
        Assert.Equal(7, await db.EducationalResources.CountAsync());
        Assert.Equal(7, StoredFiles().Length);
    }

    [Fact]
    public async Task Seed_RecreatesMissingDemoFiles()
    {
        await SeedAsync();
        File.Delete(StoredFiles()[0]);

        var result = await SeedAsync();

        Assert.Equal(1, result.RestoredFiles);
        Assert.Equal(7, StoredFiles().Length);
        await using var db = Factory.CreateDbContext();
        foreach (var resource in await db.EducationalResources.ToListAsync())
            Assert.True(File.Exists(Path.Combine(Factory.UploadRoot, resource.StoredFileName)));
    }

    [Fact]
    public async Task Seed_ReusesExistingUniversityWithSameName()
    {
        await using (var db = Factory.CreateDbContext())
        {
            db.Universities.Add(new University { Name = "Ege Üniversitesi", City = "İzmir" });
            await db.SaveChangesAsync();
        }

        var result = await SeedAsync();

        Assert.Equal(2, result.Universities);
        await using var check = Factory.CreateDbContext();
        Assert.Equal(1, await check.Universities.CountAsync(university => university.Name == "Ege Üniversitesi"));
    }

    [Fact]
    public async Task Seed_ProducesStructurallyValidFiles()
    {
        await SeedAsync();
        await using var db = Factory.CreateDbContext();
        var resources = await db.EducationalResources.ToListAsync();

        foreach (var resource in resources)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(Factory.UploadRoot, resource.StoredFileName));
            Assert.Equal(resource.FileSize, bytes.Length);
            switch (resource.ContentType)
            {
                case "application/pdf":
                    AssertValidPdf(bytes);
                    break;
                case "image/png":
                    AssertValidPng(bytes);
                    break;
                default:
                    Assert.Equal("text/plain", resource.ContentType);
                    Assert.Contains("demo", Encoding.UTF8.GetString(bytes), StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        Assert.Contains(resources, resource => resource.ContentType == "application/pdf");
        Assert.Contains(resources, resource => resource.ContentType == "image/png");
        Assert.Equal(4, resources.Select(resource => resource.Type).Distinct().Count());
    }

    private async Task<DemoDataSeeder.DemoSeedResult> SeedAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FileStorage:RootPath"] = Factory.UploadRoot })
            .Build();
        var storage = new LocalFileStorageService(configuration, new HostingEnvironment { ContentRootPath = Path.GetTempPath() });
        await using var db = Factory.CreateDbContext();
        return await DemoDataSeeder.SeedAsync(db, storage, NullLogger.Instance);
    }

    private static void AssertValidPdf(byte[] bytes)
    {
        var text = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.EndsWith("%%EOF\n", text);
        var startXref = int.Parse(text[(text.LastIndexOf("startxref\n", StringComparison.Ordinal) + 10)..].Split('\n')[0]);
        Assert.StartsWith("xref", text[startXref..]);
        var entries = text[startXref..].Split('\n').Skip(3).TakeWhile(line => line.EndsWith(" n ")).ToArray();
        Assert.Equal(5, entries.Length);
        for (var index = 0; index < entries.Length; index++)
            Assert.StartsWith($"{index + 1} 0 obj", text[int.Parse(entries[index][..10])..]);
    }

    private static void AssertValidPng(byte[] bytes)
    {
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, bytes[..8]);
        var position = 8;
        var width = 0;
        var height = 0;
        using var idat = new MemoryStream();
        while (position < bytes.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(position));
            var type = Encoding.ASCII.GetString(bytes, position + 4, 4);
            var data = bytes.AsSpan(position + 8, length);
            var crc = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(position + 8 + length));
            Assert.Equal(TableCrc32(bytes.AsSpan(position + 4, length + 4)), crc);
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
            }
            else if (type == "IDAT")
            {
                idat.Write(data);
            }

            position += 12 + length;
        }

        idat.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
            zlib.CopyTo(inflated);
        Assert.Equal(height * (1 + width * 3), inflated.Length);
    }

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(index =>
    {
        var value = (uint)index;
        for (var bit = 0; bit < 8; bit++)
            value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
        return value;
    }).ToArray();

    private static uint TableCrc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in bytes)
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}

public sealed class DemoDataStartupTests : IAsyncLifetime
{
    private readonly CheckPointApiFactory _factory = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Development_WithDemoDataEnabled_SeedsUsableData()
    {
        _factory.EnvironmentName = "Development";
        _factory.Settings["Database:AutoMigrate"] = "true";
        _factory.Settings["Seed:DemoData"] = "true";
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "ayse.demo@example.com", password = DemoDataSeeder.DemoPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resources = await client.GetFromJsonAsync<PagedResponse<ResourceResponse>>(
            "/api/resources?pageSize=100",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            {
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });
        Assert.Equal(7, resources!.TotalCount);
        var png = resources.Items.Single(item => item.OriginalFileName == "akis-semasi.png");
        var download = await client.GetAsync($"/api/resources/{png.Id}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("Development", "false")]
    [InlineData("Testing", "true")]
    public async Task DemoData_IsSkippedOutsideDevelopmentOrWhenDisabled(string environment, string demoData)
    {
        await _factory.CreateDatabaseAsync();
        _factory.EnvironmentName = environment;
        _factory.Settings["Seed:DemoData"] = demoData;

        _factory.CreateClient();

        await using var db = _factory.CreateDbContext();
        Assert.False(await db.Universities.AnyAsync());
        Assert.False(await db.Users.AnyAsync(user => user.Role == "User"));
    }
}
