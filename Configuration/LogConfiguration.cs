using Maynard.Logging;
using Maynard.Logging.Throttling;
using Maynard.Time;

namespace Maynard.Configuration;

internal class LogConfiguration
{
    internal int ThrottleThreshold { get; set; }
    internal int ThrottleWindowInSeconds { get; set; }
    internal int BufferSize { get; set; }
    internal int FlushIntervalInSeconds { get; set; }
    internal Severity MinimumSeverity { get; set; }
    internal Type OwnersEnumType { get; set; }
    internal Dictionary<int, string> OwnerNames { get; set; }
    internal int DefaultOwner { get; set; }
    internal EventHandler<LogData> OnReroute { get; set; }
    internal EventHandler<LogData[]> OnFlush { get; set; }
    internal Throttler<string> Throttler { get; set; }
    internal bool UseThrottling => Throttler != null;
    internal bool UseFlushing => BufferSize > 0;
    internal bool RerouteLogs => RerouteIndividualLogs || RerouteBulkLogs;
    internal bool RerouteBulkLogs => OnFlush != null;
    internal bool RerouteIndividualLogs => OnReroute != null;
    internal PriorityQueue<LogData, long> Queue { get; set; } = new();
    internal bool DisableWrap { get; set; }
    internal TimestampDisplaySettings TimestampDisplaySetting { get; set; }
    internal bool IsConfigured { get; set; }
}