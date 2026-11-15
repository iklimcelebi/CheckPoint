namespace CheckPoint.Api.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const string StagedDeletionSuffix = ".deleted";
    private const int BufferSize = 81920;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg"
    };

    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredPath = configuration["FileStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = "uploads";

        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(environment.ContentRootPath, configuredPath));
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file extension.", nameof(extension));

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = GetSafePath(storedFileName);

        FileStream destination;
        try
        {
            Directory.CreateDirectory(_rootPath);
            destination = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true);
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            throw StorageUnavailableException.From(exception);
        }

        try
        {
            var buffer = new byte[BufferSize];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                await WriteAsync(() => destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken));

            await WriteAsync(() => new ValueTask(destination.FlushAsync(cancellationToken)));
            await WriteAsync(destination.DisposeAsync);
            return storedFileName;
        }
        catch
        {
            await DisposeQuietlyAsync(destination);
            TryDelete(path);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storedFileName);

        try
        {
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            throw StorageUnavailableException.From(exception);
        }
    }

    public Task<IStagedFileDeletion> StageDeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storedFileName);
        if (!File.Exists(path))
            return Task.FromResult<IStagedFileDeletion>(NothingToDelete.Instance);

        var stagedPath = path + StagedDeletionSuffix;
        try
        {
            File.Move(path, stagedPath, overwrite: true);
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            throw StorageUnavailableException.From(exception);
        }

        return Task.FromResult<IStagedFileDeletion>(new StagedDeletion(path, stagedPath));
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storedFileName);

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            throw StorageUnavailableException.From(exception);
        }

        return Task.CompletedTask;
    }

    private string GetSafePath(string storedFileName)
    {
        var extension = Path.GetExtension(storedFileName);
        var baseName = Path.GetFileNameWithoutExtension(storedFileName);
        if (!string.Equals(Path.GetFileName(storedFileName), storedFileName, StringComparison.Ordinal) ||
            !Guid.TryParseExact(baseName, "N", out _) ||
            !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid stored file name.", nameof(storedFileName));
        }

        return Path.Combine(_rootPath, storedFileName);
    }

    private static bool IsStorageError(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;

    private static async ValueTask WriteAsync(Func<ValueTask> write)
    {
        try
        {
            await write();
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            throw StorageUnavailableException.From(exception);
        }
    }

    private static async ValueTask DisposeQuietlyAsync(FileStream stream)
    {
        try
        {
            await stream.DisposeAsync();
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
        }
    }

    private sealed class StagedDeletion(string originalPath, string stagedPath) : IStagedFileDeletion
    {
        public Task CompleteAsync(CancellationToken cancellationToken)
        {
            try
            {
                File.Delete(stagedPath);
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                throw StorageUnavailableException.From(exception);
            }

            return Task.CompletedTask;
        }

        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                File.Move(stagedPath, originalPath, overwrite: false);
            }
            catch (Exception exception) when (IsStorageError(exception))
            {
                throw StorageUnavailableException.From(exception);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class NothingToDelete : IStagedFileDeletion
    {
        public static readonly NothingToDelete Instance = new();

        public Task CompleteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RestoreAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
