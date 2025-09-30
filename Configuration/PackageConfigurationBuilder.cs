using Maynard.Logging;

namespace Maynard.Configuration;

public class PackageConfigurationBuilder : Builder
{
    public PackageConfigurationBuilder ConfigureLogging(Action<LogConfigurationBuilder> logs) => OnceOnly<PackageConfigurationBuilder>(() =>
    {
        logs.Invoke(new());
        Log.Configuration.IsConfigured = true;
        Log.FlushStartupLogs().Wait();
        Log.Good("Logging configured successfully!");
    });

    // public PackageConfigurationBuilder ConfigureMinq(Action<MinqConfigurationBuilder> minq) => OnceOnly<PackageConfigurationBuilder>(() =>
    // {
    //     minq.Invoke(new());
    // });
}





