using System.Text;
using Maynard.Interfaces;
using Maynard.Json;
using Maynard.Logging;

namespace Maynard.Web;

public sealed class FlexRequestBuilder(string baseUri, Func<FlexRequestBuilder, string, Task<FlexJson>> execute) : FlexRequest, IValidator, IDisposable
{
    private FlexJson _query;
    internal FlexJson _body;
    internal bool ThrowOnErrors { get; set; } = true;
    internal bool ThrowOnTimeout { get; set; } = true;
    internal int _maxRetries;
    internal int _timeoutInSeconds = 30;
    internal bool _sendAsFormData = false;
    private string _url;
    internal HttpRequestMessage _request = new();
    internal Action<FlexRequestResult> _onSuccess;
    internal Action<FlexRequestResult> _onError;
    internal Action<FlexRequestResult> _onTimeout;
    internal CancellationToken _token;
    private Func<FlexRequestBuilder, string, Task<FlexJson>> _execute = execute;
    private static readonly HttpMethod[] HasBody = [HttpMethod.Patch, HttpMethod.Post, HttpMethod.Put];

    #region Configuration

    public override FlexRequestBuilder AddAuthorization(string token) => AppendHeader("Authorization", $"Bearer {token}");

    public override FlexRequestBuilder SendAsFormData()
    {
        _sendAsFormData = true;
        return this;
    }

    public override FlexRequestBuilder AppendHeader(string key, string value)
    {
        _request.Headers.Add(key, value);
        return this;
    }

    public override FlexRequestBuilder AppendHeaders(FlexJson headers)
    {
        if (headers != null)
            foreach (string key in headers.Keys)
                AppendHeader(key, headers.Optional<string>(key));
        return this;
    }

    public override FlexRequestBuilder AppendQuery(string key, string value)
    {
        _query ??= new();
        _query[key] = value;
        return this;
    }

    public override FlexRequestBuilder SetQuery(FlexJson headers)
    {
        if (headers != null)
            _query = headers;
        return this;
    }

    public override FlexRequestBuilder DoNotThrowOnErrors()
    {
        ThrowOnErrors = false;
        return this;
    }

    public override FlexRequestBuilder DoNotThrowOnTimeout()
    {
        ThrowOnTimeout = false;
        return this;
    }

    public override FlexRequestBuilder SetBody(FlexJson body)
    {
        if (body != null)
            _body = body;
        return this;
    }

    public override FlexRequestBuilder SetCancellationToken(CancellationToken token)
    {
        if (token != CancellationToken.None)
            _token = token;
        return this;
    }

    public override FlexRequestBuilder SetRetries(int retries)
    {
        _maxRetries = retries;
        return this;
    }

    public override FlexRequestBuilder SetTimeout(int seconds)
    {
        _timeoutInSeconds = seconds;
        return this;
    }

    public override FlexRequestBuilder SetUrl(string url)
    {
        _url = url;
        return this;
    }

    public override FlexRequestBuilder OnSuccess(Action<FlexRequestResult> callback)
    {
        _onSuccess += callback;
        return this;
    }

    public override FlexRequestBuilder OnError(Action<FlexRequestResult> callback)
    {
        _onError += callback;
        return this;
    }

    public override FlexRequestBuilder OnTimeout(Action<FlexRequestResult> callback)
    {
        _onTimeout += callback;
        return this;
    }
    #endregion Configuration
    
    #region Asynchronous Methods
    public override Task<FlexJson> ConnectAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(ConnectAsync));

    public override Task<FlexJson> DeleteAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(DeleteAsync));

    public override Task<FlexJson> GetAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(GetAsync));
    public override Task<FlexJson> HeadAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(HeadAsync));

    public override Task<FlexJson> OptionsAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(OptionsAsync));

    public override Task<FlexJson> PatchAsync(string url, FlexJson body = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetBody(body)
        .SetCancellationToken(token)
        .Build(nameof(PatchAsync));

    public override Task<FlexJson> PostAsync(string url, FlexJson body = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetBody(body)
        .SetCancellationToken(token)
        .Build(nameof(PostAsync));

    public override Task<FlexJson> PutAsync(string url, FlexJson body = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetBody(body)
        .SetCancellationToken(token)
        .Build(nameof(PutAsync));

    public override Task<FlexJson> TraceAsync(string url, FlexJson query = null, CancellationToken token = default) => 
        SetUrl(url)
        .SetQuery(query)
        .SetCancellationToken(token)
        .Build(nameof(TraceAsync));
    #endregion Asynchronous Methods

    public void Validate(out List<string> errors)
    {
        errors = [];
        if (string.IsNullOrWhiteSpace(_url))
            errors.Add("URL is required.");
        if (_maxRetries < 0)
            errors.Add("Max Retries must be greater than or equal to 0.");
        else if (_maxRetries > 10)
            Log.Warn("Max Retries is greater than 10.  This may cause performance issues, especially if the API call is blocking execution.", data: new
            {
                Url = _url,
                MaxRetries = _maxRetries
            });
        if (_timeoutInSeconds < 0)
            errors.Add("Timeout must be greater than or equal to 0.");
        else if (_timeoutInSeconds < 5)
            Log.Warn("Timeout for a request was set to a relatively small value - was this intentional?", new
            {
                Url = _url,
                TimeoutInSeconds = _timeoutInSeconds
            });
        
        ThrowOnErrors &= _onError == null;
        ThrowOnTimeout &= _onTimeout == null;
    }

    private async Task<FlexJson> Build(string method)
    {
        Validate(out List<string> errors);
        if (!_url.StartsWith("http"))
            _url = $"{baseUri.TrimEnd('/')}/{_url.TrimStart('/')}";
        if (errors.Any())
        {
            Log.Error("Invalid request configuration.  The request will not be sent.", data: new
            {
                Url = _url,
                Errors = errors
            });
            return null;
        }
        
        _request.Method = method switch
        {
            nameof(ConnectAsync) => HttpMethod.Connect,
            nameof(DeleteAsync) => HttpMethod.Delete,
            nameof(GetAsync) => HttpMethod.Get,
            nameof(HeadAsync) => HttpMethod.Head,
            nameof(OptionsAsync) => HttpMethod.Options,
            nameof(PatchAsync) => HttpMethod.Patch,
            nameof(PostAsync) => HttpMethod.Post,
            nameof(PutAsync) => HttpMethod.Put,
            nameof(TraceAsync) => HttpMethod.Trace,
            _ => throw new NotImplementedException()
        };

        // Some systems disallow JSON bodies on certain kinds of requests.  For these cases, we'll move the body to the
        // query string instead.  Warning: serialization here can be problematic.
        if (_body != null && !HasBody.Contains(_request.Method))
        {
            Log.Warn("A body was specified for an HTTP method that doesn't traditionally support bodies.  The body will be converted to a query string.", new
            {
                Url = _url,
            });
            if (_query != null)
                _query.Combine(_body);
            else
                _query = _body;
            _body = null;
        }
        
        // Build out the query string.
        StringBuilder url = new();
        url.Append(_url);
        if (_query != null && _query.Any())
        {
            url.Append('?');
            foreach (string key in _query.Keys)
                url.Append($"{key}={Uri.EscapeDataString(_query.Optional<string>(key))}&");
            url.Remove(url.Length - 1, 1);
        }
        _request.RequestUri = new Uri(url.ToString(), UriKind.RelativeOrAbsolute);


        _request.Content = _body switch
        {
            _ when _body == null => null,
            _ when _sendAsFormData => _body.ConvertToFormData(),
            _ => new StringContent(_body, Encoding.UTF8, "application/json")
        };
        
        return await _execute(this, method);
    }

    public void Dispose()
    {
        _request?.Dispose();
    }
}