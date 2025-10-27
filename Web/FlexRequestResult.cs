using System.Net;
using Maynard.Json;

namespace Maynard.Web;

public class FlexRequestResult
{
    public string Url { get; set; }
    public FlexJson Data { get; set; }
    public string Error { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public int StatusCodeAsInt => (int) StatusCode;
    public int Attempts { get; set; }
    public int Retries => Attempts - 1;
    public int MaxRetries { get; set; }
    public bool IsSuccess => StatusCodeAsInt is >= 200 and < 300;
    public long ElapsedMs { get; set; }
    public Exception Exception { get; set; }
}