using System.Net;
using CheckPoint.Api.Data;
using CheckPoint.Api.Models;
using CheckPoint.Api.Services;
using CheckPoint.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CheckPoint.Api.Tests;

public sealed class StorageConsistencyTests : ApiTestBase
{
    [Fact]
    public async Task Upload_WhenDatabaseSaveFails_RemovesStoredFile()
    {
        var interceptor = new FailingResourceSaveInterceptor();
        Factory.ConfigureServices = services =>
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(interceptor));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        interceptor.FailOn = EntityState.Added;

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Empty(StoredFiles());
        await using var db = Factory.CreateDbContext();
        Assert.False(await db.EducationalResources.AnyAsync());
    }

    [Fact]
    public async Task Delete_WhenDatabaseSaveFails_RestoresFileAndKeepsRecord()
    {
        var interceptor = new FailingResourceSaveInterceptor();
        Factory.ConfigureServices = services =>
            services.ConfigureDbContext<AppDbContext>(options => options.AddInterceptors(interceptor));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var file = TestFiles.Pdf();
        var resource = await UploadAsync(admin.Client, university.Id, course.Id, file);
        var storedBefore = Assert.Single(StoredFiles());
        interceptor.FailOn = EntityState.Deleted;

        var response = await admin.Client.DeleteAsync($"/api/resources/{resource.Id}");

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal(storedBefore, Assert.Single(StoredFiles()));
        interceptor.FailOn = null;
        var download = await admin.Client.GetAsync($"/api/resources/{resource.Id}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(file.Content, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Delete_WhenStorageUnavailable_Returns503AndKeepsRecordAndFile()
    {
        var failures = new StorageFailures();
        UseFailingStorage(failures);
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        failures.Stage = true;

        var response = await admin.Client.DeleteAsync($"/api/resources/{resource.Id}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal(StorageUnavailableException.UnavailableMessage, problem.Detail);
        Assert.Single(StoredFiles());
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.GetAsync($"/api/resources/{resource.Id}")).StatusCode);
    }

    [Fact]
    public async Task Delete_WhenFinalFileRemovalFails_DeletesRecordAndLogsWarning()
    {
        var failures = new StorageFailures();
        var logs = new CapturingLoggerProvider();
        UseFailingStorage(failures, services => services.AddSingleton<ILoggerProvider>(logs));
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        failures.Complete = true;

        var response = await admin.Client.DeleteAsync($"/api/resources/{resource.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.Client.GetAsync($"/api/resources/{resource.Id}")).StatusCode);
        Assert.Contains(logs.Logs, log =>
            log.Level == LogLevel.Warning && log.Message.Contains($"Resource {resource.Id} was deleted"));
    }

    [Fact]
    public async Task Upload_WhenDiskIsFull_Returns503WithTurkishMessageAndStoresNothing()
    {
        var failures = new StorageFailures { Save = StorageUnavailableException.DiskFullMessage };
        UseFailingStorage(failures);
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);

        var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

        var problem = await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Hizmet kullanılamıyor", problem.Title);
        Assert.Equal(StorageUnavailableException.DiskFullMessage, problem.Detail);
        Assert.Empty(StoredFiles());
        await using var db = Factory.CreateDbContext();
        Assert.False(await db.EducationalResources.AnyAsync());
    }

    [Fact]
    public async Task Upload_WhenRootPathIsUnusable_Returns503()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Factory.UploadRoot)!);
        var blockingFile = Factory.UploadRoot + ".dosya";
        await File.WriteAllTextAsync(blockingFile, "klasör değil");
        Factory.Settings["FileStorage:RootPath"] = blockingFile;
        try
        {
            var admin = await CreateAdminAsync();
            var (university, course) = await CreateCatalogAsync(admin);

            var response = await UploadRawAsync(admin.Client, university.Id, course.Id, TestFiles.Text());

            var problem = await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
            Assert.Equal(StorageUnavailableException.UnavailableMessage, problem.Detail);
            await using var db = Factory.CreateDbContext();
            Assert.False(await db.EducationalResources.AnyAsync());
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }

    [Fact]
    public async Task Download_WhenStorageUnavailable_Returns503()
    {
        var failures = new StorageFailures();
        UseFailingStorage(failures);
        var admin = await CreateAdminAsync();
        var (university, course) = await CreateCatalogAsync(admin);
        var resource = await UploadAsync(admin.Client, university.Id, course.Id);
        failures.Open = true;

        var response = await admin.Client.GetAsync($"/api/resources/{resource.Id}/download");

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public void DiskFullErrors_AreRecognized()
    {
        var diskFull = OperatingSystem.IsWindows()
            ? new IOException("disk full", unchecked((int)0x80070070))
            : new IOException("disk full", 28);

        Assert.True(StorageUnavailableException.IsDiskFullError(diskFull));
        Assert.True(StorageUnavailableException.From(diskFull).IsDiskFull);
        Assert.False(StorageUnavailableException.IsDiskFullError(new IOException("başka hata")));
        Assert.False(StorageUnavailableException.From(new UnauthorizedAccessException()).IsDiskFull);
    }

    private void UseFailingStorage(StorageFailures failures, Action<IServiceCollection>? configure = null)
    {
        Factory.ConfigureServices = services =>
        {
            services.AddSingleton<LocalFileStorageService>();
            services.AddSingleton<IFileStorageService>(provider =>
                new FailingFileStorage(provider.GetRequiredService<LocalFileStorageService>(), failures));
            configure?.Invoke(services);
        };
    }

    private sealed class StorageFailures
    {
        public string? Save { get; init; }
        public bool Open { get; set; }
        public bool Stage { get; set; }
        public bool Complete { get; set; }
    }

    private sealed class FailingFileStorage(IFileStorageService inner, StorageFailures failures) : IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken) =>
            failures.Save is not null
                ? throw new StorageUnavailableException(failures.Save)
                : inner.SaveAsync(content, extension, cancellationToken);

        public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken) =>
            failures.Open
                ? throw new StorageUnavailableException(StorageUnavailableException.UnavailableMessage)
                : inner.OpenReadAsync(storedFileName, cancellationToken);

        public async Task<IStagedFileDeletion> StageDeleteAsync(string storedFileName, CancellationToken cancellationToken)
        {
            if (failures.Stage)
                throw new StorageUnavailableException(StorageUnavailableException.UnavailableMessage);

            var staged = await inner.StageDeleteAsync(storedFileName, cancellationToken);
            return failures.Complete ? new FailingCompletion(staged) : staged;
        }

        public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken) =>
            inner.DeleteAsync(storedFileName, cancellationToken);
    }

    private sealed class FailingCompletion(IStagedFileDeletion inner) : IStagedFileDeletion
    {
        public Task CompleteAsync(CancellationToken cancellationToken) =>
            throw new StorageUnavailableException(StorageUnavailableException.UnavailableMessage);

        public Task RestoreAsync(CancellationToken cancellationToken) => inner.RestoreAsync(cancellationToken);
    }

    private sealed class FailingResourceSaveInterceptor : SaveChangesInterceptor
    {
        public EntityState? FailOn { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (FailOn is { } state &&
                eventData.Context!.ChangeTracker.Entries<EducationalResource>().Any(entry => entry.State == state))
            {
                throw new DbUpdateException("Simulated database failure.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
