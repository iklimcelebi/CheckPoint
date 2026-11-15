namespace CheckPoint.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task<IStagedFileDeletion> StageDeleteAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}

public interface IStagedFileDeletion
{
    Task CompleteAsync(CancellationToken cancellationToken);

    Task RestoreAsync(CancellationToken cancellationToken);
}
