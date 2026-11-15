using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CheckPoint.Api.Tests.Infrastructure;

public sealed record CapturedLog(
    string Category,
    LogLevel Level,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> State,
    string? Exception)
{
    public string AllText =>
        string.Join(" | ", new[] { Category, Message, Exception ?? string.Empty }
            .Concat(State.Select(pair => $"{pair.Key}={pair.Value}")));

    public object? Value(string key) => State.FirstOrDefault(pair => pair.Key == key).Value;
}

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLog> _logs = new();

    public IReadOnlyCollection<CapturedLog> Logs => _logs.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _logs);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var pairs = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
            logs.Enqueue(new CapturedLog(category, logLevel, formatter(state, exception), pairs.ToArray(), exception?.ToString()));
        }
    }
}
