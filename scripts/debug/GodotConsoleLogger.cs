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
        GD.Print($"[{categoryName}] {logLevel}: {message}");

        // TODO: Print warning
    }
}
