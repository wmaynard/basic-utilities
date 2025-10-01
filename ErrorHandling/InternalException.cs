namespace Maynard.ErrorHandling;

internal class InternalException(string message, ErrorCode errorCode, object data = null) : Exception(message)
{
    public ErrorCode ErrorCode { get; set; } = errorCode;
    public object AdditionalData { get; set; } = data;
}