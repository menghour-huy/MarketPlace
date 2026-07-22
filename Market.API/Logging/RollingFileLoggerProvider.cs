using System.Collections.Concurrent;

namespace Market.API.Logging;

public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, RollingFileLogger> _loggers = new();

    public RollingFileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new RollingFileLogger(name, _logDirectory));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}

internal sealed class RollingFileLogger : ILogger
{
    private static readonly object Sync = new();
    private readonly string _categoryName;
    private readonly string _logDirectory;

    public RollingFileLogger(string categoryName, string logDirectory)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var filePath = Path.Combine(_logDirectory, $"market-api-{DateTime.UtcNow:yyyyMMdd}.log");
        var message = formatter(state, exception);
        var logEntry =
            $"{DateTime.UtcNow:O} [{logLevel}] {_categoryName}: {message}{Environment.NewLine}" +
            (exception is null ? string.Empty : $"{exception}{Environment.NewLine}");

        lock (Sync)
        {
            File.AppendAllText(filePath, logEntry);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
