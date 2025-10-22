using System.Text;
using Maynard.Configuration;
using Maynard.Json;
using Maynard.Logging;
using Microsoft.AspNetCore.Builder;

namespace Maynard.Extensions;

public static class WebApplicationExtension
{
    public static WebApplication ConfigureMaynardTools(this WebApplication app, Action<Configuration.MaynardConfigurationBuilder> builder)
    {
        Log.Info("Configuring Maynard Tools...");
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
        MaynardConfigurationBuilder config = new();
        builder.Invoke(config);

        if (!Log.Configuration.IsConfigured)
        {
            StringBuilder sb = new();
            sb.Append("MaynardTools logging was not configured.  Logs will be rerouted to Console for debug builds and ignored during release builds.");
            sb.Append(Environment.NewLine);
            sb.Append($"To clean up these console messages, call the extension method {nameof(config.ConfigureLogging)}() from the MaynardConfigurationBuilder method chain.");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            
            Console.WriteLine(sb.ToString());
            #if DEBUG
            config.ConfigureLogging(logs => logs.Reroute((_, log) => Console.WriteLine($"{log.Severity} {log.Message}")));
            _ = Log.FlushStartupLogs();
            #elif RELEASE
            Log.Disable();
            #endif
        }
        
        if (ServiceCollectionExtension.InitializeSingletons(app.Services) == 0)
            Log.Warn("No singletons were initialized; ensure you've called builder.Services.AddMaynardTools() before configuring your app.");
        
        app.UseCors(ServiceCollectionExtension.CorsPolicyName);
        
        Log.Good("Maynard Tools configured successfully!");
        return app;
    }
}