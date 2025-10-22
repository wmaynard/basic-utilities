using System.Reflection;
using Maynard.Json.Utilities;
using Maynard.Logging;
using Maynard.Singletons;
using Maynard.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Maynard.Extensions;

public static class ServiceCollectionExtension
{
    internal static readonly string CorsPolicyName = "MaynardTools"; // TODO: use nuget package name
    private static bool _added;
    private static Type[] _singletons = [];
    public static IServerSideBlazorBuilder AddMaynardTools(this IServerSideBlazorBuilder builder, WebApplicationBuilder webApp = null)
    {
        builder.Services.AddMaynardTools(webApp?.GetBaseUrl());
        return builder;
    }

    public static IServiceCollection AddMaynardTools(this IServiceCollection services, string baseUrl)
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
            ?? [];
        
        foreach (Type type in _singletons)
            services.AddSingleton(type);
        _added = true;
        

        Log.Verbose("Adding support for cookies, FlexApiClient, and FlexApiJsClient");
        services.AddCors(options =>
        {
            options
                .AddPolicy(CorsPolicyName, policy => policy
                    .WithOrigins(baseUrl)
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                );
        })
        .AddScoped(provider =>
        {
            IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
            FlexApiJsClient jsClient = provider.GetRequiredService<FlexApiJsClient>();

            return new CookieMonster(jsClient, accessor);
        })
        .AddScoped(provider =>
        {
            IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
            IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
            HttpRequest request = accessor.HttpContext?.Request;
            
            string baseUrl = request != null 
                ? $"{request.Scheme}://{request.Host}{request.PathBase}"
                : "/";
            return new FlexApiClient(factory, baseUrl);
        })
        .AddScoped(provider =>
        {
            IJSRuntime js = provider.GetRequiredService<IJSRuntime>();
            IHttpContextAccessor httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            HttpRequest request = httpContextAccessor.HttpContext?.Request;
    
            string baseUrl = request != null 
                ? $"{request.Scheme}://{request.Host}{request.PathBase}"
                : "/";
    
            return new FlexApiJsClient(js, baseUrl);
        })
        .AddHttpClient(nameof(FlexApiClient), (provider, client) =>
        {
            IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
            HttpRequest request = accessor.HttpContext?.Request;

            string baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}{request.PathBase}"
                : "/";

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            HttpClientHandler output = new()
            {
                UseCookies = true,
                CookieContainer = new()
            };
            #if DEBUG
            Log.Verbose("Debug only: Accepting any server certificate validator.");
            output.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            #endif
            return output;
        });

        Log.Verbose("Adding Controllers");
        services.AddControllers();
        
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