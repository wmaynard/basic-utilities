using System.Reflection;
using Maynard.Configuration;
using Maynard.Diagnostics;
using Maynard.Logging.Throttling;
using Maynard.Time;
using static Maynard.Logging.Severity;

namespace Maynard.Logging;

public static class Log
{

    private static readonly Lock _Door = new();
    internal static LogConfiguration Configuration { get; set; } = new();

    // No owner, data optional
    public static void Good(string message, object data = null) => _ = Validate(null, Severity.Good, message, data, null);
    public static void Verbose(string message, object data = null) => _ = Validate(null, Severity.Verbose, message, data, null);
    public static void Info(string message, object data = null) => _ = Validate(null, Severity.Info, message, data, null);
    public static void Warn(string message, object data = null) => _ = Validate(null, Severity.Warn, message, data, null);
    public static void Error(string message, object data = null) => _ = Validate(null, Severity.Error, message, data, null);
    public static void Alert(string message, object data = null) => _ = Validate(null, Severity.Alert, message, data, null);
    
    // No Owner, Exception provided
    public static void Good(string message, Exception exception) => _ = Validate(null, Severity.Good, message, null, exception);
    public static void Verbose(string message, Exception exception) => _ = Validate(null, Severity.Verbose, message, null, exception);
    public static void Info(string message, Exception exception) => _ = Validate(null, Severity.Info, message, null, exception);
    public static void Warn(string message, Exception exception) => _ = Validate(null, Severity.Warn, message, null, exception);
    public static void Error(string message, Exception exception) => _ = Validate(null, Severity.Error, message, null, exception);
    public static void Alert(string message, Exception exception) => _ = Validate(null, Severity.Alert, message, null, exception);
    
    public static void Good(string message, object data, Exception exception) => _ = Validate(null, Severity.Good, message, data, exception);
    public static void Verbose(string message, object data, Exception exception) => _ = Validate(null, Severity.Verbose, message, data, exception);
    public static void Info(string message, object data, Exception exception) => _ = Validate(null, Severity.Info, message, data, exception);
    public static void Warn(string message, object data, Exception exception) => _ = Validate(null, Severity.Warn, message, data, exception);
    public static void Error(string message, object data, Exception exception) => _ = Validate(null, Severity.Error, message, data, exception);
    public static void Alert(string message, object data, Exception exception) => _ = Validate(null, Severity.Alert, message, data, exception);
    
    // Owner provided, Data optional
    
    public static void Good<T>(T owner, string message, object data = null) where T : Enum
        => _ = Validate(Convert.ToInt32(owner), Severity.Good, message, data, null);
    public static void Verbose<T>(T owner, string message, object data = null) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Verbose, message, data, null);
    public static void Info<T>(T owner, string message, object data = null) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Info, message, data, null);
    public static void Warn<T>(T owner, string message, object data = null) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Warn, message, data, null);
    public static void Error<T>(T owner, string message, object data = null) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Error, message, data, null);
    public static void Alert<T>(T owner, string message, object data = null) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Alert, message, data, null);
    
    // Owner & Exception provided
    public static void Good<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Good, message, null, exception);
    public static void Verbose<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Verbose, message, null, exception);
    public static void Info<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Info, message, null, exception);
    public static void Warn<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Warn, message, null, exception);
    public static void Error<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Error, message, null, exception);
    public static void Alert<T>(T owner, string message, Exception exception) where T : Enum 
        => _ = Validate(Convert.ToInt32(owner), Severity.Alert, message, null, exception);
    

    private static async Task Validate(int? ownerId, Severity severity, string message, object data, Exception exception)
    {
        if (Configuration.IsDisabled)
            return;
        ownerId ??= Configuration.DefaultOwner;
        
        if (!Configuration.UseThrottling)
        {
            await Enqueue((int)ownerId, severity, message, data, exception);
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
                    Task task = Enqueue((int)ownerId, severity, message, data, exception);
                    task.Wait();
                    break;
                case ThrottleStatus.Suppressed:
                default:
                    break;
            }
        });
    }

    private static async Task Enqueue(int ownerId, Severity severity, string message, object data, Exception exception)
    {
        LogData log = new()
        {
            OwnerId = ownerId,
            Severity = severity,
            Message = message,
            Timestamp = TimestampMs.Now,
            Data = data,
            Exception = exception,
            ApplicationVersion = ReflectionHelper.ApplicationVersion,
            LibraryVersion = ReflectionHelper.LibraryVersion,
            // TODO
            Url = null,
            TokenInfo = null,
        };
        
        if (!Configuration.IsConfigured)
        {
            lock (_Door)
                Configuration.Queue.Enqueue(log, log.Timestamp);
            return;
        }

        if (!Configuration.UseFlushing)
        {
            if (!Configuration.RerouteLogs)
                ToConsole(log);
            else
                Configuration.OnReroute?.Invoke(null, log);
        }
        else
        {
            lock (_Door)
                Configuration.Queue.Enqueue(log, log.Timestamp);
            if (Configuration.Queue.Count >= Configuration.BufferSize)
                Flush();
        }
    }

    private static void ToConsole(LogData log)
    {
        if (Configuration.RerouteIndividualLogs)
            Configuration.OnReroute?.Invoke(null, log);
        else
            PrettyPrintToConsole(log);
    }

    private static void PrettyPrintToConsole(LogData log)
    {
        if (Configuration.MinimumSeverity > log.Severity)
            return;
        ConsoleColor previous = Console.ForegroundColor;
        ConsoleColor previousBg = Console.BackgroundColor;
        ConsoleColor color = log.Severity switch
        {
            Severity.Good => ConsoleColor.Green,
            Severity.Verbose => ConsoleColor.Gray,
            Severity.Info => ConsoleColor.Black,
            Severity.Warn => ConsoleColor.Yellow,
            Severity.Error or Severity.Alert => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        lock (_Door)
        {
            Console.ForegroundColor = color;
            if (log.Severity == Severity.Verbose)
                Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(log);
            Console.ForegroundColor = previous;
            Console.BackgroundColor = previousBg;
        }
    }

    private static async Task Flush() => await Task.Run(() =>
    {
        Queue<LogData> toPrint = new();
        lock (_Door)
        {
            while (Configuration.Queue.TryDequeue(out LogData log, out _))
                toPrint.Enqueue(log);
        }

        if (Configuration.RerouteBulkLogs)
            Configuration.OnFlush?.Invoke(null, toPrint.ToArray());
        else
            while (toPrint.TryDequeue(out LogData log))
                ToConsole(log);
    });

    internal static async Task FlushStartupLogs()
    {
        if (!Configuration.RerouteLogs)
            Console.WriteLine(LogData.GetHeaders());
        await Flush();
    }

    internal static async Task Disable()
    {
        Configuration.IsConfigured = true;
        Configuration.IsDisabled = true;

        lock (_Door)
            Configuration.Queue.Clear();
    }
}
public enum Owner { Default = 0, Will = 1 }