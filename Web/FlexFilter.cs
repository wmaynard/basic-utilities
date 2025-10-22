using Maynard.Auth;
using Maynard.ErrorHandling;
using Maynard.Extensions;
using Maynard.Json;
using Maynard.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Maynard.Web;

public class FlexFilter : IAsyncAuthorizationFilter, IAsyncResourceFilter, IAsyncExceptionFilter
{
    internal const string KEY_TOKEN = "validatedToken";
    internal const string KEY_DATA = "requestDataAsFlexJson";
    internal const string KEY_GEODATA = "locationData";
    internal const string KEY_MODELS = "validatedModels";
    
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor)
            return;

        RequireAuth[] auths = context.GetControllerAttributes<RequireAuth>();
        NoAuth noAuth = context.GetControllerAttributes<NoAuth>().FirstOrDefault();
        AuthType combined = auths.Aggregate(AuthType.Optional, (current, auth) => current | auth.Type);
        
        if (!auths.Any() && noAuth == null)
            Log.Warn("No auth attributes found on this controller.", data: new
            {
                Help = $"All traffic will be allowed on this endpoint.  If this is intentional, add a [{nameof(NoAuth)}] attribute to the method.)",
                Endpoint = context.GetEndpoint()
            });

        context.HttpContext.Request.Headers.TryGetValue("Authorization", out StringValues authHeaders);
        string jwt = authHeaders.FirstOrDefault()?.Replace("Bearer ", "");
        TokenInfo.TryFromJwt(jwt, out TokenInfo token);
        
        bool optional = combined.HasFlag(AuthType.Optional) || noAuth != null || !auths.Any();
        bool tokenRequired = !optional && combined.HasFlag(AuthType.StandardToken);
        bool adminRequired = !optional && combined.HasFlag(AuthType.AdminToken);
        // TODO: Permissions support

        try
        {
            if (token == null)
            {
                if (optional)
                    return;
                throw new InternalException("No token present in request.", ErrorCode.InvalidToken, new
                {
                    Help = "The authorization header with JWT is either missing or incorrect.",
                    Endpoint = context.GetEndpoint()
                });
            }

            if (adminRequired && !token.IsAdmin)
                throw new InternalException("Admin token required.", ErrorCode.InvalidToken, new
                {
                    Help = "This endpoint requires an admin token.",
                    Endpoint = context.GetEndpoint()
                });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            context.HttpContext.Items[KEY_TOKEN] = token;
        }
    }
    
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        FlexJson data = context.GetRequestDataAsFlexJson();
        context.HttpContext.Items[KEY_DATA] = data;
        context.HttpContext.Items[KEY_GEODATA] = LocationData.Lookup(context.GetIpAddress());
        context.HttpContext.Items[KEY_MODELS] = Jsonifier.UnJsonify<FlexModel>(data);

        await next();
    }
    
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        Exception exception = context.Exception;
        
        Log.Verbose($"Uncaught exception: {exception.Message}", data: new
        {
            Help = "This exception has been suppressed by the framework filters.  It should be handled more gracefully.",
            Endpoint = context.GetEndpoint()
        }, exception);
        
        context.ExceptionHandled = true;

        FlexJson response = new()
        {
            { "message", "The server encountered an error, likely an unsuccessful API response." }
        };
        
        #if DEBUG
        // Note: Exception serialization can fail if you simply add the exception to the response.
        // Exceptions can contain countless objects and types that require their own custom serialization,
        // so it's best to flatten them out.
        // TODO: Explore a factory pattern to flatten exceptions in a JsonConverter.
        response["debug"] = new FlexJson
        {
            {"exception", exception.Message},
            {"stackTrace", exception.StackTrace}
        };
        #endif
        
        context.Result = new BadRequestObjectResult(response);

        // TODO: Handle specific exceptions for special error codes, e.g. auth / 403s.
    }
}

