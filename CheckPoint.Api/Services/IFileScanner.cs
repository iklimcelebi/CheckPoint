namespace CheckPoint.Api.Services;

public enum FileScanStatus
{
    Clean,
    Infected,
    Unavailable
}

public interface IFileScanner
{
    Task<FileScanStatus> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken);
}

public sealed class NoOpFileScanner : IFileScanner
{
    public Task<FileScanStatus> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken) =>
        Task.FromResult(FileScanStatus.Clean);
}
