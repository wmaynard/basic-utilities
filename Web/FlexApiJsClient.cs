using System.Net;
using System.Net.Http.Headers;
using Maynard.Json;
using Maynard.Logging;
using Maynard.Time;
using Microsoft.JSInterop;

namespace Maynard.Web;

/// <summary>
/// This might seem unnecessary with <see cref="FlexApiClient" />.  However, an important difference
/// is that all the API calls here are actually made from the JS runtime - this means that all of the calls actually happen
/// from the browser's side, not the server.  This is necessary to be able to set cookies, particularly in InteractiveServer
/// pages.
/// <br />
/// <br />
/// The primary utility of this class is in keeping all of the responsible code isolated to an @code block and not needing
/// to bother with maintaining JS functions, whether they're in a component's &lt;script&gt; tags or in some other file
/// altogether, and it also provides code completion / comments as you build requests.
/// </summary>
/// <param name="js"></param>
public class FlexApiJsClient(IJSRuntime js, string baseUri) : FlexApiClient(null, baseUri)
{
    protected override async Task<InterimResult> Send(HttpClient client, FlexRequestBuilder builder, CancellationToken token)
    {
        try
        {
            FlexJson headers = new()
            {
                { "Content-Type", "application/json" },
            };
            foreach (KeyValuePair<string, IEnumerable<string>> header in builder._request.Headers)
                headers.Add(header.Key, header.Value);
                
            string request = $$"""
                (async function() {
                   const response = await fetch('{{builder._request.RequestUri}}', {
                       method: '{{builder._request.Method}}',
                       headers: {{headers}},
                       body: '{{builder._body}}',
                       credentials: 'include'
                   });
                   const text = await response.text();
                   return { code: response.status, body: text };
               })();
               """
                .Replace("body: '',", ""); // Required for body-less methods; otherwise an exception gets thrown.
            
            FlexJson response = (await js.InvokeAsync<object>("eval", token, request)).ToString();

            return new()
            {
                Code = (HttpStatusCode)response.Require<int>("code"),
                Data = response.Require<string>("body")
            };
        }
        catch (Exception e)
        {
            return new()
            {
                Code = HttpStatusCode.InternalServerError,
                Data = new()
                {
                    { "exception", e }
                }
            };
        }
    }
}