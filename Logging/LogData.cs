using System.Text;
using Maynard.Auth;
using Maynard.ErrorHandling;
using Maynard.Time;
using Maynard.Tools.Extensions;

namespace Maynard.Logging;

public struct LogData
{
    public Severity Severity { get; set; }
    public string Message { get; set; }
    public TokenInfo TokenInfo { get; set; }
    public long Timestamp { get; set; }
    public string Url { get; set; }
    public object Data { get; set; }
    public string ApplicationVersion { get; set; }
    public string LibraryVersion { get; set; }
    public int OwnerId { get; set; }
    public Exception Exception { get; set; }

    private string OwnerName => Log.Configuration.OwnerNames.TryGetValue(OwnerId, out string name)
        ? name
        : Log.Configuration.OwnerNames[Log.Configuration.DefaultOwner];

    private static readonly int _lengthOwner = Math.Max(5, Log.Configuration.OwnerNames.Values.Max(name => name.Length));
    private static readonly int _lengthSeverity = Enum.GetNames<Severity>().Max(name => name.Length);
    private const int LENGTH_MESSAGE = 80;

    internal static string GetHeaders()
    {
        const int TIMEZONE_LENGTH = 6;
        int lengthTimestamp = Log.Configuration.TimestampDisplaySetting switch
        {
            TimestampDisplaySettings.DateTimeLocal => DATETIME_FORMAT.Length + TIMEZONE_LENGTH,
            TimestampDisplaySettings.DateTimeUtc => DATETIME_FORMAT.Length + TIMEZONE_LENGTH,
            TimestampDisplaySettings.TimeLocal => TIME_FORMAT.Length + TIMEZONE_LENGTH,
            TimestampDisplaySettings.TimeUtc => TIME_FORMAT.Length + TIMEZONE_LENGTH,
            TimestampDisplaySettings.UnixTimestamp => 10,
            TimestampDisplaySettings.UnixTimestampMs => 13,
            _ => throw new InternalException("Enum value was out of bounds", ErrorCode.EnumOutOfBounds, data: new { Type = typeof(TimestampDisplaySettings).FullName })
        };
        StringBuilder builder = new();
        builder.Append("Timestamp".PadRight(lengthTimestamp));
        builder.Append(" | ");
        builder.Append("Owner".PadLeft(_lengthOwner));
        builder.Append(" | ");
        builder.Append("Severity".PadLeft(_lengthSeverity));
        builder.Append(" | ");
        builder.Append("Message\n");
        builder.Append("".PadRight(builder.Length + 80, '-'));
        
        return builder.ToString();
    }
    
    private const string DATETIME_FORMAT = "yyyy.MM.dd HH:mm:ss.fff";
    private const string TIME_FORMAT = "HH:mm:ss.fff";
    
    // Timestamp | Owner | Severity | Message
    // 
    // Timestamp | Owner | Severity | This is a sample message that will wrap if too long
    //                                Continued message here, can be disabled with DisableLineWrap()
    public override string ToString()
    {
        StringBuilder builder = new();
        DateTime time = TimestampMs.ToDateTime(Timestamp);

        builder.Append(Log.Configuration.TimestampDisplaySetting switch
        {
            TimestampDisplaySettings.DateTimeLocal => $"[{time.GetLocalTimezoneAbbreviation()}] " + time.ToLocalTime().ToString(DATETIME_FORMAT),
            TimestampDisplaySettings.DateTimeUtc =>  "[UTC] " + time.ToString(DATETIME_FORMAT),
            TimestampDisplaySettings.TimeLocal => $"[{time.GetLocalTimezoneAbbreviation()}] " + time.ToLocalTime().ToString(TIME_FORMAT),
            TimestampDisplaySettings.TimeUtc => "[UTC] " + time.ToString(TIME_FORMAT),
            TimestampDisplaySettings.UnixTimestamp => Timestamp / 1_000,
            TimestampDisplaySettings.UnixTimestampMs => Timestamp,
            _ => throw new InternalException("Enum value was out of bounds", ErrorCode.EnumOutOfBounds, data: new { Type = typeof(TimestampDisplaySettings).FullName })
        });
        builder.Append(" | ");
        builder.Append(OwnerName.PadLeft(_lengthOwner));
        builder.Append(" | ");
        builder.Append(Severity.ToString().ToUpper().PadLeft(_lengthSeverity));

        if (Log.Configuration.DisableWrap || Message.Length <= LENGTH_MESSAGE)
        {
            builder.Append(" | ");
            builder.Append(Message);
        }
        else
        {
            builder.Append(" |");
            Queue<string> words = new(Message.Split(' '));
            int length = 0;
            int messageColumnStart = builder.Length;
            bool wordAdded = false;
            while (words.TryDequeue(out string word))
            {
                if (length + word.Length + 1 > LENGTH_MESSAGE && wordAdded)
                {
                    builder.Append(Environment.NewLine);
                    builder.Append(new string(' ', messageColumnStart - 1));
                    builder.Append('|');
                    length = 0;
                }
                builder.Append(' ');
                builder.Append(word);
                length += word.Length + 1;
                wordAdded = true;
            }
        }
        
        return builder.ToString();
    }
}