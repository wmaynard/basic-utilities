using Maynard.Auth;
using Maynard.ErrorHandling;
using Maynard.Interfaces;
using Maynard.Json;
using Maynard.Json.Exceptions;
using Maynard.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Maynard.Web;

// TODO: Create a CommonController so that we don't have copies of endpoints like /health, /cachedToken, /refresh

[ApiController]
[TypeFilter(typeof(FlexFilter))]
public abstract class FlexController : Controller, IAutocaster
{
    // TODO: Do we need to inject IServiceProvider?
    protected FlexJson Data => FromContext<FlexJson>(FlexFilter.KEY_DATA);
    protected TokenInfo Token => FromContext<TokenInfo>(FlexFilter.KEY_TOKEN); // TODO: Is it possible to make this accessible to models?
    protected LocationData Geolocation => FromContext<LocationData>(FlexFilter.KEY_GEODATA);

    [NonAction]
    private T FromContext<T>(string key, object _default = null)
    {
        try
        {
            return Request.HttpContext.Items.TryGetValue(key, out object value)
                ? (T) value
                : default;
        }
        catch (Exception e)
        {
            Log.Warn($"{key} was requested from the HttpContext but nothing was found.", data: e);
            return default;
        }
    }
    
    #region IAutocasterImplementation
    [NonAction]
    public object Optional(string key) => Optional<object>(key);

    [NonAction]
    public T Optional<T>(string key) => Data != null
        ? Data.Optional<T>(key)
        : default;

    [NonAction]
    public object Require(string key) => Require<object>(key);
    
    [NonAction]
    public T Require<T>(string key)
    {
        if (Data == null)
            throw new InternalException("The current request is missing a JSON body or query parameters.", ErrorCode.InvalidRequestData, new
            {
                Help = "This can occur from malformed JSON, FormData, or a serialization error.  Check to make sure the request JSON is valid."
            });
        return Data.Require<T>(key);
    }
    #endregion IAutocasterImplementation
}