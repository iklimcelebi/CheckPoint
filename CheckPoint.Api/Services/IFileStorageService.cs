namespace CheckPoint.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);
    FileStream OpenRead(string storedFileName);
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
