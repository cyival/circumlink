using Microsoft.Extensions.Logging;

namespace Circumlink.Debug;

public static class Log
{
    public static bool IsLoggingEnabled { get; set; } = true;

    private static readonly ILoggerFactory _factory = LoggerFactory.Create(builder =>
    {
        builder.AddGodotConsole();
    });

    private static readonly ILogger _defaultLogger = _factory.CreateLogger("app");

    public static ILogger GetLogger(string name) => _factory.CreateLogger(name);

    public static ILogger<T> GetLogger<T>() => _factory.CreateLogger<T>();

    // === Default logging methods ===

    public static void LogTrace(string message) => _defaultLogger.LogTrace(message);

    public static void LogDebug(string message) => _defaultLogger.LogDebug(message);

    public static void LogInformation(string message) => _defaultLogger.LogInformation(message);

    public static void LogWarning(string message) => _defaultLogger.LogWarning(message);

    public static void LogError(string message) => _defaultLogger.LogError(message);

    public static void LogCritical(string message) => _defaultLogger.LogCritical(message);
}
