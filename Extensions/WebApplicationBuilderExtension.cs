using System.Reflection;
using Maynard.Logging;
using Microsoft.AspNetCore.Builder;

namespace Maynard.Extensions;

public static class WebApplicationBuilderExtension
{
    public static string GetBaseUrl(this WebApplicationBuilder builder)
    {
        // e.g. https://localhost:3013;http://localhost:3012
        string baseUrl = builder?.Configuration.GetSection("URLS").Value
            ?? builder?.Configuration.GetSection("ASPNETCORE_URLS").Value
            ?? builder?.Host.Properties.Values.Select(property =>
            {
                PropertyInfo urls = property.GetType().GetProperties().FirstOrDefault(prop => prop.Name.Equals("ServerUrls"));
                object value = urls?.GetValue(property);

                try
                {
                    return (string)value;
                }
                catch
                {
                    return null;
                }
            }).FirstOrDefault(str => !string.IsNullOrWhiteSpace(str));

        string[] urls = baseUrl?.Split(';');

        baseUrl = urls?.FirstOrDefault(url => url.StartsWith("https://")) ?? urls?.FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Log.Warn("Unable to get base URL from reflection on the supplied WebApplicationBuilder.", new
            {
                Help = "You may need to supply the base URL directly for certain features, like CORS, to work correctly."
            });
        }
        return baseUrl;
    }
}