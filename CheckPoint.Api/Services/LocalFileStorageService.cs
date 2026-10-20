namespace CheckPoint.Api.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
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

        Directory.CreateDirectory(_rootPath);
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = GetSafePath(storedFileName);

        try
        {
            await using var destination = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);
            await content.CopyToAsync(destination, cancellationToken);
            return storedFileName;
        }
        catch
        {
            if (File.Exists(path))
                File.Delete(path);

            throw;
        }
    }

    public FileStream OpenRead(string storedFileName)
    {
        var path = GetSafePath(storedFileName);
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetSafePath(storedFileName);
        if (File.Exists(path))
            File.Delete(path);

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
}
