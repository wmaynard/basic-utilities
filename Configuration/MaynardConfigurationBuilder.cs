using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Maynard.Auth;
using Maynard.Extensions;
using Maynard.Json.Extensions;
using Maynard.Json.Utilities;
using Maynard.Logging;
using Maynard.Singletons;
using Maynard.Time;
using Microsoft.AspNetCore.Http;
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

    public MaynardConfigurationBuilder ConfigureAuth(Action<AuthConfigurationBuilder> auth) => OnceOnly<MaynardConfigurationBuilder>(() =>
    {
        auth.Invoke(new());
        if (string.IsNullOrWhiteSpace(JwtHelper.Config.ServerAudience))
        {
            Log.Warn("No audience specified; trying to use a default value from the HTTP context.");
            HttpContextAccessor accessor = new();
            JwtHelper.Config.ServerAudience = $"{accessor.HttpContext?.Request.Scheme}://{accessor.HttpContext?.Request.Host}";
        }
        if (string.IsNullOrWhiteSpace(JwtHelper.Config.ServerAudience))
        {
            Log.Warn("No issuer specified; trying to use a default value from the HTTP context.");
            HttpContextAccessor accessor = new();
            JwtHelper.Config.ServerIssuer = $"{accessor.HttpContext?.Request.Scheme}://{accessor.HttpContext?.Request.Host}";
        }

        if (string.IsNullOrWhiteSpace(JwtHelper.Config.PrivateKey) || string.IsNullOrWhiteSpace(JwtHelper.Config.PublicKey))
        {
            Log.Warn("A private or public key was not found for JWT authentication.  A new private / public key pair will be generated.  This is not recommended for production use.", new
            {
                Help = "Use the AuthConfigurationBuilder to set the private / public keys (PEM format).",
                Detail = "Generated keys do not persist and are unique to each instance of the application.  Auth will work, but if deployed in a cluster with multiple instances, you will see auth errors."
            });
            JwtHelper.UseTemporaryKeyPair();
        }

        if (JwtHelper.Config.LifetimeInSeconds <= 0)
        {
            Log.Warn("Token TTL was not specified, or was invalid.  Defaulting to 1 hour.");
            JwtHelper.Config.LifetimeInSeconds = Interval.OneHour;
        }

        if (JwtHelper.Config.CacheLifetimeInSeconds < 0)
            JwtHelper.Config.CacheLifetimeInSeconds = 0;
        JwtHelper.Config.Validate();
    });

    public MaynardConfigurationBuilder ConfigureJsonSerialization(JsonOptions options) => OnceOnly<MaynardConfigurationBuilder>(() =>
    {
        JsonHelper.SerializerOptions.ApplyTo(options.JsonSerializerOptions);
    });
}


