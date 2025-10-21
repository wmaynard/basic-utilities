using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Maynard.Extensions;
using Maynard.Interfaces;
using Maynard.Json;
using Maynard.Logging;
using Maynard.Time;
using Microsoft.AspNetCore.Http;

namespace Maynard.Web;

public class FlexApiClient(IHttpClientFactory httpClientFactory, string baseUri) : FlexRequest
{
    private static int Jitter => Random.Shared.Next(0, 100);
    private string BaseUri { get; set; } = baseUri;
    public override FlexRequestBuilder AppendHeader(string key, string value) => CreateBuilder().AppendHeader(key, value);
    public override FlexRequestBuilder AppendHeaders(FlexJson headers) => CreateBuilder().AppendHeaders(headers);
    public override FlexRequestBuilder AppendQuery(string key, string value) => CreateBuilder().AppendQuery(key, value);
    public override FlexRequestBuilder SetQuery(FlexJson headers) => CreateBuilder().SetQuery(headers);
    public override FlexRequestBuilder DoNotThrowOnErrors() => CreateBuilder().DoNotThrowOnErrors();
    public override FlexRequestBuilder DoNotThrowOnTimeout() => CreateBuilder().DoNotThrowOnTimeout();
    public override FlexRequestBuilder SetBody(FlexJson body) => CreateBuilder().SetBody(body);
    public override FlexRequestBuilder SetCancellationToken(CancellationToken token) => CreateBuilder().SetCancellationToken(token);
    public override FlexRequestBuilder SetRetries(int retries) => CreateBuilder().SetRetries(retries);
    public override FlexRequestBuilder SetTimeout(int seconds) => CreateBuilder().SetTimeout(seconds);
    public override FlexRequestBuilder SetUrl(string url) => CreateBuilder().SetUrl(url);
    public override FlexRequestBuilder OnSuccess(Action<FlexRequestResult> callback) => CreateBuilder().OnSuccess(callback);
    public override FlexRequestBuilder OnError(Action<FlexRequestResult> callback) => CreateBuilder().OnError(callback);
    public override FlexRequestBuilder OnTimeout(Action<FlexRequestResult> callback) => CreateBuilder().OnTimeout(callback);
    public override Task<FlexJson> ConnectAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().ConnectAsync(url, query, token);
    public override Task<FlexJson> DeleteAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().DeleteAsync(url, query, token);
    public override Task<FlexJson> GetAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().GetAsync(url, query, token);
    public override Task<FlexJson> HeadAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().HeadAsync(url, query, token);
    public override Task<FlexJson> OptionsAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().OptionsAsync(url, query, token);
    public override Task<FlexJson> PatchAsync(string url, FlexJson body = null, CancellationToken token = default) => CreateBuilder().PatchAsync(url, body, token);
    public override Task<FlexJson> PostAsync(string url, FlexJson body = null, CancellationToken token = default) => CreateBuilder().PostAsync(url, body, token);
    public override Task<FlexJson> PutAsync(string url, FlexJson body = null, CancellationToken token = default) => CreateBuilder().PutAsync(url, body, token);
    public override Task<FlexJson> TraceAsync(string url, FlexJson query = null, CancellationToken token = default) => CreateBuilder().TraceAsync(url, query, token);
    
    private FlexRequestBuilder CreateBuilder()
    {
        FlexRequestBuilder builder = new(BaseUri, SendAsync);
        return builder;
    }

    protected virtual async Task<InterimResult> Send(HttpClient client, FlexRequestBuilder builder, CancellationToken token)
    {
        HttpResponseMessage response = await client.SendAsync(builder._request, token);
        return new()
        {
            Code = response.StatusCode,
            Data = await response.Content.ReadAsStringAsync(token)
        };
    }

    protected struct InterimResult
    {
        public InterimResult() { }
        public HttpStatusCode Code { get; set; } = 0;
        public FlexJson Data { get; set; } = null;
        public long Timestamp { get; private set; } = TimestampMs.Now;
    }
    
    protected async Task<FlexJson> SendAsync(FlexRequestBuilder builder, string method)
    {
        FlexRequestResult result = new()
        {
            MaxRetries = builder._maxRetries
        };
        long timestamp = TimestampMs.Now;
        int retriesRemaining = result.MaxRetries;
        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(builder._timeoutInSeconds));
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(builder._token, timeoutCts.Token);
        using HttpClient client = httpClientFactory?.CreateClient(nameof(FlexApiClient));
        
        do
        {
            try
            {
                // Exponential backoff: 2^n * 100ms
                // A small amount of jitter (up to 100ms) is added to prevent thundering herds.
                // Retries    | 0   1     2     3     4     5
                // Delay (ms) | 0   200   400   800   1600  3200
                if (result.Attempts++ > 0)
                {
                    Log.Verbose("Request failed, but has remaining retries available.", new
                    {
                        Url = builder._request.RequestUri
                    });
                    await Task.Delay((int) Math.Pow(2, result.Retries) * 100 + Jitter, linkedCts.Token);
                }
                
                InterimResult interim = await Send(client, builder, linkedCts.Token);
                result.StatusCode = interim.Code;
                result.Data = interim.Data;
                result.ElapsedMs = interim.Timestamp - timestamp;

                // HttpResponseMessage response = await client.SendAsync(builder._request, linkedCts.Token);
                // result.StatusCode = response.StatusCode;
                // result.Data = await response.Content.ReadAsStringAsync(linkedCts.Token);
                // result.ElapsedMs = TimestampMs.Now - timestamp;
            }
            catch (OperationCanceledException e) when (timeoutCts.IsCancellationRequested)
            {
                result.Error = "Request timed out.";
                result.Exception = e;

                Log.Error(e.Message, data: new
                {
                    Url = builder._request.RequestUri
                }, e);
                builder._onTimeout?.Invoke(result);
                if (builder.ThrowOnTimeout)
                    throw;
            }
            catch (OperationCanceledException e)
            {
                result.Error = "Request was cancelled by caller.";
                result.Exception = e;

                Log.Error(e.Message, data: new
                {
                    Url = builder._request.RequestUri
                }, e);
                builder._onTimeout?.Invoke(result);
                if (builder.ThrowOnTimeout)
                    throw;
            }
            catch (Exception e)
            {
                result.Error = "Unable to successfully send or parse response.";
                result.Exception = e;
                Log.Error(e.Message, data: new
                {
                    Url = builder._request.RequestUri
                }, e);
                builder._onError?.Invoke(result);
                if (e is not TaskCanceledException && builder.ThrowOnErrors)
                    throw;
            }
            finally
            {
                if (result.IsSuccess)
                    builder._onSuccess?.Invoke(result);
            }
        } while (!result.IsSuccess && result.StatusCodeAsInt.IsNotBetween(400, 499) && retriesRemaining-- > 0);
        
        builder.Dispose();

        return result.Data;
    }
}

