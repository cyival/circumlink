using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Circumlink.Debug;

public static class GodotConsoleLoggerExtensions
{
    public static ILoggingBuilder AddGodotConsole(
        this ILoggingBuilder builder)
    {
        //builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, GodotConsoleLoggerProvider>());

        /*LoggerProviderOptions.RegisterProviderOptions
            <GodotConsoleLoggerConfiguration, GodotConsoleLoggerProvider>(builder.Services);*/

        return builder;
    }

    /*public static ILoggingBuilder AddGodotConsole(
        this ILoggingBuilder builder,
        Action<GodotConsoleLoggerConfiguration> configure)
    {
        builder.AddGodotConsoleLogger();
        builder.Services.Configure(configure);

        return builder;
    }*/
}
