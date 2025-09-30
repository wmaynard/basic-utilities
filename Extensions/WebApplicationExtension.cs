using Maynard.Configuration;
using Maynard.Json;
using Maynard.Logging;
using Microsoft.AspNetCore.Builder;

namespace Maynard.Extensions;

public static class WebApplicationExtension
{
    public static WebApplication ConfigureMaynardTools(this WebApplication app, Action<PackageConfigurationBuilder> builder)
    {
        FlexJson.Configure(logEvent =>
        {
            (logEvent.Severity switch
            {
                FlexJsonLogEventArgs.GOOD => Log.Good(logEvent.Message, logEvent.Data),
                FlexJsonLogEventArgs.VERBOSE => Log.Verbose(logEvent.Message, logEvent.Data),
                FlexJsonLogEventArgs.INFO => Log.Info(logEvent.Message, logEvent.Data),
                FlexJsonLogEventArgs.WARN => Log.Warn(logEvent.Message, logEvent.Data),
                FlexJsonLogEventArgs.ERROR => Log.Error(logEvent.Message, logEvent.Data),
                FlexJsonLogEventArgs.ALERT => Log.Alert(logEvent.Message, logEvent.Data),
                _ => Log.Error("Unexpected log severity!", new { LogEvent = logEvent })
            }).Wait();
        });
        Log.Verbose("FlexJson log events captured and tied to logging.").Wait();
        builder.Invoke(new());
        Log.Good("Maynard Tools configured successfully!").Wait();
        return app;
    }
}