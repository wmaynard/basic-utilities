using System.Reflection;
using Maynard.Json.Utilities;
using Maynard.Logging;
using Maynard.Singletons;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Maynard.Extensions;

public static class ServiceCollectionExtension
{
    private static bool _added;
    private static Type[] _singletons = [];
    public static IServerSideBlazorBuilder AddMaynardTools(this IServerSideBlazorBuilder builder)
    {
        builder.Services.AddMaynardTools();
        return builder;
    }

    public static IServiceCollection AddMaynardTools(this IServiceCollection services)
    {
        if (_added)
        {
            Log.Warn("Already added singletons.  Ignoring subsequent calls.");
            return services;
        }
        Log.Verbose("Creating service singletons");
        _singletons = Assembly
            .GetEntryAssembly()
            ?.GetExportedTypes() // Add the project's types 
            .Concat(Assembly.GetExecutingAssembly().GetExportedTypes()) // Add platform-common's types
            .Where(type => !type.IsAbstract)
            .Where(type => type.IsAssignableTo(typeof(Singleton)))
            .ToArray()
            ?? Array.Empty<Type>();
        
        foreach (Type type in _singletons)
            services.AddSingleton(type);
        _added = true;
        
        Log.Verbose("Adding JSON customizations");
        services.AddControllers().AddJsonOptions(JsonHelper.ConfigureJsonOptions);
        
        return services;
    }

    internal static int InitializeSingletons(IServiceProvider provider)
    {
        foreach (Type type in _singletons)
            provider.GetService(type);
        return _singletons.Length;
    }
}