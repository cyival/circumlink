using System;
using Godot;
using Microsoft.Extensions.Logging;

namespace Circumlink.Debug;

public sealed class GodotConsoleLogger(string categoryName) : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel) => Debug.Log.IsLoggingEnabled;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        //if (config.EventId == 0 || config.EventId == eventId.Id)

        var message = formatter(state, exception);
        GD.Print($"[{categoryName}] {GetShortLogLevel(logLevel)}:\n    {message}");

        // TODO: Print warning
    }

    private static string GetShortLogLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        LogLevel.None => "NONE",
        _ => logLevel.ToString(),
    };
}
