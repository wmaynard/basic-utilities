using Maynard.Auth;
using Microsoft.AspNetCore.Http;

namespace Maynard.Extensions;

public static class IHttpContextAccessorExtension
{
    public static string GetJwt(this IHttpContextAccessor accessor) => accessor?.HttpContext?.Request.Cookies[TokenInfo.KEY_COOKIE_NAME];

    public static TokenInfo ValidateJwt(this IHttpContextAccessor accessor)
    {
        string jwt = accessor.GetJwt();
        
        return !string.IsNullOrWhiteSpace(jwt)
            ? TokenInfo.FromJwt(jwt)
            : null;
    }
}