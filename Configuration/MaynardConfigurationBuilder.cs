using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Extensions;
using Maynard.Json.Utilities;
using Maynard.Logging;
using Maynard.Singletons;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Maynard.Configuration;

// Extend this class for derived libraries to add their own configuration options.
public class MaynardConfigurationBuilder : Builder
{
    public MaynardConfigurationBuilder ConfigureLogging(Action<LogConfigurationBuilder> logs) => OnceOnly<MaynardConfigurationBuilder>(() =>
    {
        logs.Invoke(new());
        Log.Configuration.Printing.Validate();
        Log.Configuration.IsConfigured = true;
        Log.FlushStartupLogs().Wait();
        Log.Good("Logging configured successfully!");
    });

    public MaynardConfigurationBuilder ConfigureJsonSerialization(JsonOptions options) => OnceOnly<MaynardConfigurationBuilder>(() =>
    {
        JsonHelper.SerializerOptions.ApplyTo(options.JsonSerializerOptions);
    });
}





