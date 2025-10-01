using System.Reflection;
using Maynard.Extensions;
using Maynard.Logging;
using Maynard.Singletons;
using Microsoft.Extensions.DependencyInjection;

namespace Maynard.Configuration;

// Extend this class for derived libraries to add their own configuration options.
public class MaynardConfigurationBuilder : Builder
{
    public MaynardConfigurationBuilder ConfigureLogging(Action<LogConfigurationBuilder> logs) => OnceOnly<MaynardConfigurationBuilder>(() =>
    {
        logs.Invoke(new());
        Log.Configuration.IsConfigured = true;
        Log.FlushStartupLogs().Wait();
        Log.Good("Logging configured successfully!");
    });
}





