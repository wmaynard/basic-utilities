using System.Net;
using Maynard.Json;

namespace Maynard.Web;

public interface IFlexRequest
{
    public FlexRequestBuilder AppendHeader(string key, string value);
    public FlexRequestBuilder AppendHeaders(FlexJson headers);
    public FlexRequestBuilder AppendQuery(string key, string value);
    public FlexRequestBuilder SetQuery(FlexJson headers);
    public FlexRequestBuilder DoNotThrowOnErrors();
    public FlexRequestBuilder DoNotThrowOnTimeout();
    public FlexRequestBuilder SetBody(FlexJson body);
    public FlexRequestBuilder SetCancellationToken(CancellationToken token);
    public FlexRequestBuilder SetRetries(int retries);
    public FlexRequestBuilder SetTimeout(int seconds);
    public FlexRequestBuilder SetUrl(string url);
    public FlexRequestBuilder OnSuccess(Action<FlexRequestResult> callback);
    public FlexRequestBuilder OnError(Action<FlexRequestResult> callback);
    public FlexRequestBuilder OnTimeout(Action<FlexRequestResult> callback);
    
    public FlexJson Connect(string url, FlexJson query = null) => Await(ConnectAsync(url, query));
    public FlexJson Delete(string url, FlexJson query = null) => Await(DeleteAsync(url, query));
    public FlexJson Get(string url, FlexJson query = null) => Await(GetAsync(url, query));
    public FlexJson Head(string url, FlexJson query = null) => Await(HeadAsync(url, query));
    public FlexJson Options(string url, FlexJson query = null) => Await(OptionsAsync(url, query));
    public FlexJson Patch(string url, FlexJson body = null) => Await(PatchAsync(url, body));
    public FlexJson Post(string url, FlexJson body = null) => Await(PostAsync(url, body));
    public FlexJson Put(string url, FlexJson body = null) => Await(PutAsync(url, body));
    public FlexJson Trace(string url, FlexJson query = null) => Await(TraceAsync(url, query));
    
    public Task<FlexJson> ConnectAsync(string url, FlexJson query = null, CancellationToken token = default);
    public Task<FlexJson> DeleteAsync(string url, FlexJson query = null, CancellationToken token = default);
    public Task<FlexJson> GetAsync(string url, FlexJson query = null, CancellationToken token = default);
    public Task<FlexJson> HeadAsync(string url, FlexJson query = null, CancellationToken token = default);
    public Task<FlexJson> OptionsAsync(string url, FlexJson query = null, CancellationToken token = default);
    public Task<FlexJson> PatchAsync(string url, FlexJson body = null, CancellationToken token = default);
    /// <summary>
    /// Sends a POST request to the specified URL with an optional JSON body.  If a body has previously been set and is
    /// provided here, it will be overwritten by the new body.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<FlexJson> PostAsync(string url, FlexJson body = null, CancellationToken token = default);
    public Task<FlexJson> PutAsync(string url, FlexJson body = null, CancellationToken token = default);
    public Task<FlexJson> TraceAsync(string url, FlexJson query = null, CancellationToken token = default);

    private static FlexJson Await(Task<FlexJson> task) => task.GetAwaiter().GetResult();
}