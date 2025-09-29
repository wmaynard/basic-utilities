using Maynard.Configuration;
using Maynard.Logging;
using Microsoft.AspNetCore.Builder;

namespace Maynard.Extensions;

public static class WebApplicationExtension
{
    public static WebApplication ConfigureMaynardTools(this WebApplication app, Action<PackageConfigurationBuilder> builder)
    {
        builder.Invoke(new());
        Log.Good("Maynard Tools configured successfully!").Wait();
        return app;
    }
}