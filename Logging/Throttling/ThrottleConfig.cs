namespace Maynard.Logging.Throttling;

public struct ThrottleConfig
{
    public int Threshold { get; set; }
    public int WindowInSeconds { get; set; }
}