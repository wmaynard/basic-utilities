using System.Reflection;
using Maynard.Logging;
using Microsoft.AspNetCore.Builder;

namespace Maynard.Extensions;

public static class WebApplicationBuilderExtension
{
    // TODO: This method currently returns the deployed site URL, but really should return the localhost URL.
    // To elaborate, when deployed on a server, using the site URL can cause the request to make a round trip to a load balancer.
    // Since we're running the app locally already, it's far more efficient to skip this trip and just hit the local endpoints directly.
    // In these cases, HTTP may also be acceptable, since the request doesn't leave the local machine.
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
            Log.Warn("Unable to get base URL from reflection on the supplied WebApplicationBuilder.", new
            {
                Help = "You may need to supply the base URL directly for certain features, like CORS, to work correctly."
            });
        #if RELEASE
        else if (baseUrl.StartsWith("http://"))
        {
            baseUrl = baseUrl.Replace("http://", "https://");
            Log.Warn("Base URL was detected as HTTP, but HTTPS is required for security.  HTTPS will be used instead.", new
            {
                Help = "If your internal API calls begin to fail, you may need to debug Maynard.Utilities."
            });
        }
        #endif
        return baseUrl;
    }
}