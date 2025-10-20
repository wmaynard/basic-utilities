using System.Net;
using Maynard.Json;

namespace Maynard.Web;

public abstract class FlexRequest
{
    #region Configuration
    public abstract FlexRequestBuilder AppendHeader(string key, string value);
    public abstract FlexRequestBuilder AppendHeaders(FlexJson headers);
    public abstract FlexRequestBuilder AppendQuery(string key, string value);
    public abstract FlexRequestBuilder SetQuery(FlexJson headers);
    public abstract FlexRequestBuilder DoNotThrowOnErrors();
    public abstract FlexRequestBuilder DoNotThrowOnTimeout();
    public abstract FlexRequestBuilder SetBody(FlexJson body);
    public abstract FlexRequestBuilder SetCancellationToken(CancellationToken token);
    public abstract FlexRequestBuilder SetRetries(int retries);
    public abstract FlexRequestBuilder SetTimeout(int seconds);
    public abstract FlexRequestBuilder SetUrl(string url);
    public abstract FlexRequestBuilder OnSuccess(Action<FlexRequestResult> callback);
    public abstract FlexRequestBuilder OnError(Action<FlexRequestResult> callback);
    public abstract FlexRequestBuilder OnTimeout(Action<FlexRequestResult> callback);
    #endregion Configuration
    
    #region Synchronous Methods
    public FlexJson Connect(string url, FlexJson query = null) => Await(ConnectAsync(url, query));
    public FlexJson Delete(string url, FlexJson query = null) => Await(DeleteAsync(url, query));
    public FlexJson Get(string url, FlexJson query = null) => Await(GetAsync(url, query));
    public FlexJson Head(string url, FlexJson query = null) => Await(HeadAsync(url, query));
    public FlexJson Options(string url, FlexJson query = null) => Await(OptionsAsync(url, query));
    public FlexJson Patch(string url, FlexJson body = null) => Await(PatchAsync(url, body));
    public FlexJson Post(string url, FlexJson body = null) => Await(PostAsync(url, body));
    public FlexJson Put(string url, FlexJson body = null) => Await(PutAsync(url, body));
    public FlexJson Trace(string url, FlexJson query = null) => Await(TraceAsync(url, query));
    #endregion Synchronous Methods
    
    #region Asynchronous Methods
    public abstract Task<FlexJson> ConnectAsync(string url, FlexJson query = null, CancellationToken token = default);
    public abstract Task<FlexJson> DeleteAsync(string url, FlexJson query = null, CancellationToken token = default);
    public abstract Task<FlexJson> GetAsync(string url, FlexJson query = null, CancellationToken token = default);
    public abstract Task<FlexJson> HeadAsync(string url, FlexJson query = null, CancellationToken token = default);
    public abstract Task<FlexJson> OptionsAsync(string url, FlexJson query = null, CancellationToken token = default);
    public abstract Task<FlexJson> PatchAsync(string url, FlexJson body = null, CancellationToken token = default);
    /// <summary>
    /// Sends a POST request to the specified URL with an optional JSON body.  If a body has previously been set and is
    /// provided here, it will be overwritten by the new body.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="body"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public abstract Task<FlexJson> PostAsync(string url, FlexJson body = null, CancellationToken token = default);
    public abstract Task<FlexJson> PutAsync(string url, FlexJson body = null, CancellationToken token = default);
    public abstract Task<FlexJson> TraceAsync(string url, FlexJson query = null, CancellationToken token = default);
    #endregion Asynchronous Methods

    #region FlexModel-Specific Methods
    public FlexJson PatchModels(string url, params FlexModel[] models) => Patch(url, Jsonifier.Jsonify(models));
    public FlexJson PostModels(string url, params FlexModel[] models) => Post(url, Jsonifier.Jsonify(models));
    public FlexJson PutModels(string url, params FlexModel[] models) => Put(url, Jsonifier.Jsonify(models));
    public Task<FlexJson> PatchModelsAsync(string url, params FlexModel[] models) => PatchModelsAsync(url, CancellationToken.None, models);
    public Task<FlexJson> PatchModelsAsync(string url, CancellationToken token = default, params FlexModel[] models) => PatchAsync(url, Jsonifier.Jsonify(models), token);
    public Task<FlexJson> PostModelsAsync(string url, params FlexModel[] models) => PostModelsAsync(url, CancellationToken.None, models);
    public Task<FlexJson> PostModelsAsync(string url, CancellationToken token = default, params FlexModel[] models) => PostAsync(url, Jsonifier.Jsonify(models), token);
    public Task<FlexJson> PutModelsAsync(string url, params FlexModel[] models) => PutModelsAsync(url, CancellationToken.None, models);
    public Task<FlexJson> PutModelsAsync(string url, CancellationToken token = default, params FlexModel[] models) => PutAsync(url, Jsonifier.Jsonify(models), token);
    #endregion FlexModel-Specific Methods
    
    
    
    
    private static FlexJson Await(Task<FlexJson> task) => task.GetAwaiter().GetResult();
}