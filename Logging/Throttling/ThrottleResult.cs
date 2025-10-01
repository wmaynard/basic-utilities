namespace Maynard.Logging.Throttling;

public struct ThrottleResult
{
    public ThrottleStatus Status { get; set; }
    public int ObjectsSuppressed { get; set; }
}