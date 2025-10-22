using System.Net;
using System.Web;
using Maynard.Auth;
using Maynard.Logging;
using Microsoft.AspNetCore.Http;

namespace Maynard.Web;

// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬛⬛⬛⬜⬜⬜⬛⬛⬛⬜⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬜⬜⬜⬛⬛⬛⬜⬜⬜⬛⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬜⬜⬛⬛⬜⬛⬜⬜⬜⬜⬜⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬜⬜⬛⬛⬜⬛⬜⬛⬛⬜⬜⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬜⬜⬜⬜⬜⬛⬜⬛⬛⬜⬜⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛⬛⬛⬛⬛⬛⬜⬜⬜⬛🟦⬛⬜⬜⬜⬛⬛⬛⬛⬛⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦⬛⬛⬛⬛🟦⬛⬛⬛⬛🟦🟦🟦🟦🟦⬛⬛⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜
// ⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜
// ⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬛⬛⬛⬛⬛⬛⬜⬜
// ⬜⬜⬜⬜⬜⬜⬛⬛🟦🟦🟦🟦⬛⬛🟦🟦🟦🟦🟦🟦🟦⬛⬛⬛🟦🟦🟦🟦🟦🟦⬛⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦⬛⬛⬛⬛⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜
// ⬜⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛🟦⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬜⬛🟦🟦⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬛⬛⬛⬛⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬛🟦🟦🟦🟦🟦🟦⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬛🟦🟦⬛⬛⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬜⬛🟦⬛🟧🟧🟧🟧🟧⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬜⬛🟦⬛🟧🟧🟫🟧🟧🟫🟧🟧⬛🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬛🟦⬛🟧🟧🟧🟧🟧🟧🟧🟧🟧🟧⬛🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬜⬛🟦⬛🟧🟧🟧⬛⬛⬛⬛⬛⬛🟧⬛🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬛🟦🟦⬛🟧🟧🟧⬛🟦🟦🟦🟦🟦⬛⬛⬛🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬜⬛🟦🟦⬛🟧🟫🟧🟧⬛⬛🟦🟦🟦🟦🟦🟦⬛⬛🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬛🟦🟦🟦⬛🟧🟧🟧⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜⬜
// ⬜⬜⬛🟦🟦🟦⬛🟧🟧🟧⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛🟦🟦⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬛🟦🟦🟦🟦🟦⬛🟧⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛🟦⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬛🟦🟦🟦🟦🟦⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬜⬜⬜⬜⬜⬜⬜
// ⬜⬛🟦🟦🟦🟦🟦🟦⬛⬛⬛⬛⬛⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛⬜⬜⬜⬜⬜⬜
// ⬜⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬛🟦🟦🟦🟦🟦🟦🟦🟦🟦⬛⬜⬜⬜⬜⬜⬜

public class CookieMonster(FlexApiJsClient client, IHttpContextAccessor context)
{
    private string Token => TokenInfo.FromJwt(Grab("authToken")).RawJwt;
    private static readonly string StoreCookieEndpoint = RouteHelper.GetRoutePath<CookiesController>(nameof(CookiesController.StoreCookie));
    private static readonly string RemoveCookieEndpoint = RouteHelper.GetRoutePath<CookiesController>(nameof(CookiesController.RemoveCookie));

    public async Task<bool> Bake(string key, string value) => await Bake(new Cookie
    {
        Key = key,
        Value = value
    });
    public async Task<bool> Bake(Cookie cookie) => await Bake(null, [cookie]);
    public async Task<bool> Bake(string token, Cookie cookie) => await Bake(token, [cookie]);
    public async Task<bool> Bake(params Cookie[] cookies) => await Bake(null, cookies);
    public async Task<bool> Bake(string token, params Cookie[] cookies)
    {
        bool success = false;
        try
        {
            await client
                .AddAuthorization(token ?? Token)
                .OnTimeout(_ => Log.Error("Request timed out setting cookie(s)"))
                .OnError(result => Log.Error("Could not set cookie(s).", result))
                .OnSuccess(_ => success = true)
                .PutModelsAsync(StoreCookieEndpoint, cookies);
        }
        catch { }
        return success;
    }
    
    public async Task<bool> Munch(string token, string key) => await Munch(token, [key]);
    public async Task<bool> Munch(params string[] keys) => await Munch(null, keys);
    public async Task<bool> Munch(string token, params string[] keys)
    {
        bool success = false;
        try
        {
            await client
                .AddAuthorization(token ?? Token)
                .OnTimeout(_ => Log.Error("Request timed out deleting cookie(s)"))
                .OnError(result => Log.Error("Could not delete cookie(s).", result))
                .OnSuccess(_ => success = true)
                .DeleteAsync(RemoveCookieEndpoint, new()
                {
                    { "keys", keys }
                });
        }
        catch { }
        return success;
    }

    public string Grab(string key) => HttpUtility.UrlDecode(context.HttpContext?.Request.Cookies[HttpUtility.UrlEncode(key)]);
}