using Maynard.Configuration;
using Maynard.Logging.Throttling;
using Maynard.Time;

namespace Maynard.Logging;

public static class Log
{
    private static readonly Lock _door = new();
    internal static LogConfiguration Configuration { get; set; } = new();

    // No owner, data optional
    public static async Task Verbose(string message, object data = null)
        => await Validate(null, Severity.Verbose, message);
    public static async Task Info(string message, object data = null)
        => await Validate(null, Severity.Info, message);
    public static async Task Warn(string message, object data = null)
        => await Validate(null, Severity.Warn, message);
    public static async Task Error(string message, object data = null)
        => await Validate(null, Severity.Error, message);
    public static async Task Critical(string message, object data = null)
        => await Validate(null, Severity.Critical, message);
    public static async Task Good(string message, object data = null)
        => await Validate(null, Severity.Good, message);
    
    // No Owner, Exception provided
    public static async Task Verbose(string message, Exception exception) 
        => await Verbose(message, data: new { Exception = exception });
    public static async Task Info(string message, Exception exception) 
        => await Info(message, data: new { Exception = exception });
    public static async Task Warn(string message, Exception exception) 
        => await Warn(message, data: new { Exception = exception });
    public static async Task Error(string message, Exception exception) 
        => await Error(message, data: new { Exception = exception });
    public static async Task Critical(string message, Exception exception) 
        => await Critical(message, data: new { Exception = exception });
    public static async Task Good(string message, Exception exception)
        => await Good(message, data: new { Exception = exception });
    
    // Owner provided, Data optional
    public static async Task Verbose<T>(T owner, string message, object data = null) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Verbose, message, data);
    public static async Task Info<T>(T owner, string message, object data = null) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Info, message, data);
    public static async Task Warn<T>(T owner, string message, object data = null) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Warn, message, data);
    public static async Task Error<T>(T owner, string message, object data = null) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Error, message, data);
    public static async Task Critical<T>(T owner, string message, object data = null) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Critical, message, data);
    public static async Task Good<T>(T owner, string message, object data = null) where T : Enum
        => await Validate(Convert.ToInt32(owner), Severity.Good, message, data);
    
    // Owner & Exception provided
    public static async Task Verbose<T>(T owner, string message, Exception exception) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Verbose, message, data: new { Exception = exception });
    public static async Task Info<T>(T owner, string message, Exception exception) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Info, message, data: new { Exception = exception });
    public static async Task Warn<T>(T owner, string message, Exception exception) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Warn, message, data: new { Exception = exception });
    public static async Task Error<T>(T owner, string message, Exception exception) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Error, message, data: new { Exception = exception });
    public static async Task Critical<T>(T owner, string message, Exception exception) where T : Enum 
        => await Validate(Convert.ToInt32(owner), Severity.Critical, message, data: new { Exception = exception });
    public static async Task Good<T>(T owner, string message, Exception exception) where T : Enum
        => await Validate(Convert.ToInt32(owner), Severity.Good, message, data: new { Exception = exception });

    // TODO: data should be FlexJson equivalent
    private static async Task Validate(int? ownerId, Severity severity, string message, object data = null)
    {
        ownerId ??= Configuration.DefaultOwner;
        
        if (!Configuration.UseThrottling)
        {
            await Enqueue((int)ownerId, severity, message, data);
            return;
        }
        
        await Configuration.Throttler.Check(message, callback: args =>
        {
            switch (args.Status)
            {
                case ThrottleStatus.PreviouslySuppressed:
                    message = $"{message} [Suppressed {args.ObjectsSuppressed} times]"; // TODO: Add to data object, not message
                    goto case ThrottleStatus.NotSuppressed;
                case ThrottleStatus.NotSuppressed:
                    Task task = Enqueue((int)ownerId, severity, message, data);
                    task.Wait();
                    break;
                case ThrottleStatus.Suppressed:
                default:
                    break;
            }
        });
    }

    private static async Task Enqueue(int ownerId, Severity severity, string message, object data = null)
    {
        LogData log = new()
        {
            OwnerId = ownerId,
            Severity = severity,
            Message = message,
            Timestamp = TimestampMs.Now,
            Data = data,
            
            // TODO
            ApplicationVersion = null,
            LibraryVersion = null,
            Url = null,
            TokenInfo = null,
        };
        if (!Configuration.IsConfigured)
        {
            lock (_door)
                Configuration.Queue.Enqueue(log, log.Timestamp);
            return;
        }

        if (!Configuration.UseFlushing)
        {
            if (!Configuration.RerouteLogs)
                await ToConsole(log);
            else
                Configuration.OnReroute?.Invoke(null, log);
        }
        else
        {
            lock (_door)
                Configuration.Queue.Enqueue(log, log.Timestamp);
            if (Configuration.Queue.Count >= Configuration.BufferSize)
                await Flush();
        }
    }

    private static async Task ToConsole(LogData log)
    {
        if (Configuration.RerouteIndividualLogs)
            Configuration.OnReroute?.Invoke(null, log);
        else
            await PrettyPrintToConsole(log);
    }

    private static async Task PrettyPrintToConsole(LogData log)
    {
        ConsoleColor previous = Console.ForegroundColor;
        ConsoleColor previousBg = Console.BackgroundColor;
        ConsoleColor color = log.Severity switch
        {
            Severity.Verbose => ConsoleColor.Gray,
            Severity.Info => ConsoleColor.Black,
            Severity.Warn => ConsoleColor.Yellow,
            Severity.Error => ConsoleColor.Red,
            Severity.Critical => ConsoleColor.Red,
            Severity.Good => ConsoleColor.Green,
            _ => ConsoleColor.White
        };

        lock (_door)
        {
            Console.ForegroundColor = color;
            if (log.Severity == Severity.Verbose)
                Console.BackgroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(log);
            Console.ForegroundColor = previous;
            Console.BackgroundColor = previousBg;
        }
    }

    private static async Task Flush()
    {
        Queue<LogData> toPrint = new();
        lock (_door)
        {
            while (Configuration.Queue.TryDequeue(out LogData log, out _))
                toPrint.Enqueue(log);
        }
        if (Configuration.RerouteBulkLogs)
            Configuration.OnFlush?.Invoke(null, toPrint.ToArray());
        else
            while (toPrint.TryDequeue(out LogData log))
                await ToConsole(log);
    }

    internal static async Task FlushStartupLogs()
    {
        Console.WriteLine(Configuration.RerouteLogs
            ? "Console logs are disabled; logs have been configured to pipe to a different handler."
            : LogData.GetHeaders()
        );
        await Flush();
    }
}
public enum Owner { Default = 0, Will = 1 }