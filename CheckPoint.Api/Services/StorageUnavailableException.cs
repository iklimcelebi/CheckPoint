namespace CheckPoint.Api.Services;

public sealed class StorageUnavailableException : Exception
{
    public const string DiskFullMessage =
        "Depolama alanı dolu. Dosya şu anda kaydedilemiyor; lütfen daha sonra tekrar deneyin.";

    public const string UnavailableMessage =
        "Dosya depolama şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin.";

    private const int WindowsDiskFull = 112;
    private const int WindowsHandleDiskFull = 39;
    private const int UnixNoSpace = 28;

    public StorageUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    public bool IsDiskFull => Message == DiskFullMessage;

    public static StorageUnavailableException From(Exception exception) =>
        IsDiskFullError(exception)
            ? new StorageUnavailableException(DiskFullMessage, exception)
            : new StorageUnavailableException(UnavailableMessage, exception);

    internal static bool IsDiskFullError(Exception exception)
    {
        if (exception is not IOException ioException)
            return false;

        var code = ioException.HResult & 0xFFFF;
        return OperatingSystem.IsWindows()
            ? code is WindowsDiskFull or WindowsHandleDiskFull
            : ioException.HResult == UnixNoSpace;
    }
}
