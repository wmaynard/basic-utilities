using System.Web;
using Maynard.Auth;
using Maynard.Json;
using Maynard.Json.Attributes;
using Maynard.Json.Enums;
using Maynard.Time;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Maynard.Web;

[Route("api/[controller]"), RequireAuth]
public class CookiesController : FlexController
{
    [HttpPut]
    public Task<IActionResult> StoreCookie()
    {
        foreach (Cookie cookie in Models.OfType<Cookie>())
            HttpContext.Response.Cookies.Append(HttpUtility.UrlEncode(cookie.Key), HttpUtility.UrlEncode(cookie.Value), new()
            {
                HttpOnly = !cookie.AvailableToJs,
                Secure = cookie.SendOverHttpsOnly,
                SameSite = cookie.SameSite,
                Expires = DateTimeOffset.UtcNow.AddSeconds(cookie.LifetimeInSeconds),
                Path = cookie.Path
            });
        return Task.FromResult<IActionResult>(Ok());
    }

    [HttpDelete]
    public Task<IActionResult> RemoveCookie()
    {
        foreach (string key in Require<string[]>("keys"))
            HttpContext.Response.Cookies.Delete(key);
        return Task.FromResult<IActionResult>(Ok());
    }
}


public class Cookie : FlexModel
{
    [FlexKeys(json: "key", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public string Key { get; set; }
    [FlexKeys(json: "value", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public string Value { get; set; }
    [FlexKeys(json: "httpOnly", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public bool AvailableToJs { get; set; } = false;
    [FlexKeys(json: "isSecure", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public bool SendOverHttpsOnly { get; set; } = true;
    [FlexKeys(json: "sameSiteMode", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
    [FlexKeys(json: "lifetime", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public long LifetimeInSeconds { get; set; } = Interval.OneWeek;
    [FlexKeys(json: "path", ignore: Ignore.InBson | Ignore.WhenNullOrDefault)]
    public string Path { get; set; } = "/";

    protected override void Validate(out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrEmpty(Key))
            errors.Add("Key is required");
        if (string.IsNullOrEmpty(Value))
            errors.Add("Value is required");
    }
}