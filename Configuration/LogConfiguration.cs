using Maynard.ErrorHandling;
using Maynard.Logging;
using Maynard.Logging.Throttling;
using Maynard.Time;
using Maynard.Tools.Extensions;

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
    internal bool IsConfigured { get; set; }
    internal bool IsDisabled { get; set; }
    internal Severity MinimumExtraSeverity { get; set; }
    
    internal PrintingConfiguration Printing { get; } = new();

    internal class PrintingConfiguration
    {
        internal const string HEADER_TIMESTAMP = "Timestamp";
        internal const string HEADER_SEVERITY = "Level";
        internal const string HEADER_OWNER = "Owner";
        internal const string HEADER_MESSAGE = "Message";
        internal const string HEADER_DATA = "Data";
        internal const string HEADER_EXCEPTION = "Exception";
        
        internal bool PrintData { get; set; }
        internal bool PrintExceptions { get; set; }
        internal TimestampDisplaySettings TimestampDisplaySetting { get; set; }
        internal int LengthTimestampColumn { get; set; }
        internal int LengthSeverityColumn { get; set; }
        internal int LengthOwnerColumn { get; set; }
        internal int LengthMessageColumn { get; set; } = 150;
        
        internal string TimestampToString(long timestamp)
        {
            const string DATETIME_FORMAT = "yyyy.MM.dd HH:mm:ss.fff";
            const string TIME_FORMAT = "HH:mm:ss.fff";
        
            DateTime time = TimestampMs.ToDateTime(timestamp);
            return TimestampDisplaySetting switch
            {
                TimestampDisplaySettings.DateTimeLocal => $"[{time.GetLocalTimezoneAbbreviation()}] " + time.ToLocalTime().ToString(DATETIME_FORMAT),
                TimestampDisplaySettings.DateTimeUtc => "[UTC] " + time.ToString(DATETIME_FORMAT),
                TimestampDisplaySettings.TimeLocal => $"[{time.GetLocalTimezoneAbbreviation()}] " + time.ToLocalTime().ToString(TIME_FORMAT),
                TimestampDisplaySettings.TimeUtc => "[UTC] " + time.ToString(TIME_FORMAT),
                TimestampDisplaySettings.UnixTimestamp => $"{timestamp / 1_000}",
                TimestampDisplaySettings.UnixTimestampMs => $"{timestamp}",
                _ => throw new InternalException("Enum value was out of bounds", ErrorCode.EnumOutOfBounds, data: new {Type = typeof(TimestampDisplaySettings).FullName})
            };
        }

        internal void Validate()
        {
            LengthTimestampColumn = Math.Max(LengthTimestampColumn, HEADER_TIMESTAMP.Length + 2);
            LengthSeverityColumn = Math.Max(LengthSeverityColumn, HEADER_SEVERITY.Length + 2);
            LengthOwnerColumn = Math.Max(LengthOwnerColumn, HEADER_OWNER.Length + 2);
            LengthMessageColumn = Math.Max(LengthMessageColumn, 50);
        }
    }
}