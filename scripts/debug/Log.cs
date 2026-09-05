using Microsoft.Extensions.Logging;

namespace Circumlink.Debug;

public static class Log
{
    public static bool IsLoggingEnabled { get; set; } = true;

    private static readonly ILoggerFactory _factory = LoggerFactory.Create(builder =>
    {
        builder.AddGodotConsole();
        if (Godot.OS.HasFeature("debug"))
            builder.SetMinimumLevel(LogLevel.Debug);
    });

    private static readonly ILogger _defaultLogger = _factory.CreateLogger("Application");

    public static ILogger GetLogger(string name) => _factory.CreateLogger(name);

    public static ILogger<T> GetLogger<T>() => _factory.CreateLogger<T>();

    // === Default logging methods ===

    public static void LogTrace(string message, params object?[] args) => _defaultLogger.LogTrace(message, args);

    public static void LogDebug(string message, params object?[] args) => _defaultLogger.LogDebug(message, args);

    public static void LogInformation(string message, params object?[] args) => _defaultLogger.LogInformation(message, args);

    public static void LogWarning(string message, params object?[] args) => _defaultLogger.LogWarning(message, args);

    public static void LogError(string message, params object?[] args) => _defaultLogger.LogError(message, args);

    public static void LogCritical(string message, params object?[] args) => _defaultLogger.LogCritical(message, args);
}
