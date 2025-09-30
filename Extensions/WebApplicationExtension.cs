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
            switch (logEvent.Severity)
            {
                case FlexJsonLogEventArgs.GOOD: Log.Good(logEvent.Message, logEvent.Data); break;
                case FlexJsonLogEventArgs.VERBOSE: Log.Verbose(logEvent.Message, logEvent.Data); break;
                case FlexJsonLogEventArgs.INFO: Log.Info(logEvent.Message, logEvent.Data); break;
                case FlexJsonLogEventArgs.WARN: Log.Warn(logEvent.Message, logEvent.Data); break;
                case FlexJsonLogEventArgs.ERROR: Log.Error(logEvent.Message, logEvent.Data); break;
                case FlexJsonLogEventArgs.ALERT: Log.Alert(logEvent.Message, logEvent.Data); break;
                default: Log.Error("Unexpected log severity!", new { LogEvent = logEvent }); break;
            }
        });
        Log.Verbose("FlexJson log events captured and tied to logging.");
        builder.Invoke(new());
        Log.Good("Maynard Tools configured successfully!");
        return app;
    }
}