using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Circumlink.Debug;

[ProviderAlias("GodotConsole")]
public sealed class GodotConsoleLoggerProvider : ILoggerProvider
{
    // Holds loggers by category name to avoid creating duplicates
    private readonly ConcurrentDictionary<string, GodotConsoleLogger> _loggers =
            new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new GodotConsoleLogger(categoryName));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
