using System.Reflection;
using System.Text;
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
        Log.Verbose($"Adding MaynardTools; accepted baseUrl is {baseUrl}");
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

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                IHttpContextAccessor accessor = provider.GetRequiredService<IHttpContextAccessor>();
                Log.Warn("Base URL is empty; attempting to parse from HTTP request.");
                HttpRequest request = accessor.HttpContext?.Request;
            
                baseUrl = request != null 
                    ? $"{request.Scheme}://{request.Host}{request.PathBase}"
                    : "/";
            }
            return new FlexApiClient(factory, baseUrl);
        })
        .AddScoped(provider =>
        {
            // Add the FlexApiJsClient.  Important to note here that for release builds, we absolutely cannot
            // allow the URL to contain localhost or HTTP, as these requests are executed from end user browsers.
            
            IJSRuntime js = provider.GetRequiredService<IJSRuntime>();

            if (!string.IsNullOrWhiteSpace(baseUrl) && !baseUrl.Contains("localhost"))
                return new FlexApiJsClient(js, baseUrl);
            
            IHttpContextAccessor httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
            HttpRequest request = httpContextAccessor.HttpContext?.Request;

            if (request == null)
            {
                Log.Warn("HttpRequest was null when initializing FlexApiJsClient; this should not happen.");
                return new FlexApiJsClient(js, "/");
            }

            StringBuilder sb = new(request.Scheme);
            #if RELEASE
            if (request.Scheme == "http")
            {
                Log.Verbose("HTTP request scheme is HTTP; forcing HTTPS.");
                sb.Append('s');
            }
            #endif

            sb.Append("://");
            sb.Append(request.Host);
            sb.Append(request.PathBase);
    
            return new FlexApiJsClient(js, sb.ToString());
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