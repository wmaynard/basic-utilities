using Maynard.Json;
using Maynard.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Maynard.Extensions;

public static class ActionContextExtension
{
    public static T[] GetControllerAttributes<T>(this ActionContext context) where T : Attribute =>
        context.ActionDescriptor is ControllerActionDescriptor descriptor
            ? descriptor
                .ControllerTypeInfo
                .GetCustomAttributes(inherit: true)
                .Concat(descriptor.MethodInfo.GetCustomAttributes(inherit: true))
                .OfType<T>()
                .ToArray()
            : null;

    public static bool ControllerHasAttribute<T>(this ActionContext context) where T : Attribute => context
        .GetControllerAttributes<T>()
        ?.Any() 
        ?? false;

    public static string GetEndpoint(this ActionContext context) => context?.HttpContext.Request.Path.Value;

    /// <summary>
    /// Converts a request's body into a FlexJson object.  By default, the query parameters are also added.  In the case
    /// of conflicts, the body values have priority.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="bodyOnly"></param>
    /// <returns></returns>
    public static FlexJson GetRequestDataAsFlexJson(this ResourceExecutingContext context, bool bodyOnly = false)
    {
        FlexJson output = new();
        try
        {
            if (!bodyOnly)
                foreach (KeyValuePair<string, StringValues> pair in context.HttpContext.Request.Query)
                    output[pair.Key] = pair.Value.ToString().Trim();
            switch (context.HttpContext.Request.Method)
            {
                case "HEAD":
                case "GET":
                case "DELETE":
                    return output;
                default:
                    if (context.HttpContext.Request.HasFormContentType)
                        foreach (KeyValuePair<string, StringValues> pair in context.HttpContext.Request.Form)
                            output[pair.Key] = pair.Value.ToString().Trim();
                    else
                    {
                        using Stream stream = context.HttpContext.Request.BodyReader.AsStream();
                        using StreamReader reader = new(stream);

                        string json = reader.ReadToEnd();
                        FlexJson temp = !string.IsNullOrWhiteSpace(json) ? json : "{}";
                        output.Combine(temp);
                    }

                    return output;
            }
        }
        catch (Exception e)
        {
            Log.Error("Unable to read request query parameters and / or body.", data: e);
            return output;
        }
    }

    public static string GetIpAddress(this ResourceExecutingContext context)
    {
        try
        {
            string GetHeader(string key)
            {
                string output = context.HttpContext.Request.Headers[key].FirstOrDefault();
                return string.IsNullOrWhiteSpace(output) ? null : output;
            }
        
            // Note: in the Groovy services, all of these keys were capitalized.
            return GetHeader("X-Real-IP")
                ?? GetHeader("X-Forwarded-For")
                ?? GetHeader("X-Original-Forwarded-For")
                ?? GetHeader("Proxy-Client-IP")
                ?? GetHeader("Client-IP")
                ?? context.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        catch (Exception e)
        {
            Log.Error("Unable to determine IP address.", e);
            return "::1";
        }
    }

    // public static T GetService<T>(this ActionContext context) where T : PlatformService => context.HttpContext.RequestServices.GetService<T>();
}