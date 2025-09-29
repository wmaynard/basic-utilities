namespace Maynard.Logging;

public struct LogBufferConfig
{
    public int BufferSize { get; set; }
    public int FlushIntervalInSeconds { get; set; }
}