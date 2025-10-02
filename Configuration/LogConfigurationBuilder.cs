using Maynard.Extensions;
using Maynard.Logging;
using Maynard.Time;

namespace Maynard.Configuration;

public class LogConfigurationBuilder : Builder
{
    private readonly List<string> _errors = new();

    public LogConfigurationBuilder AddThrottling(int threshold, int windowInSeconds) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        if (threshold < 1)
            _errors.Add($"{nameof(threshold)} must be greater than 0.  {threshold} was provided.");
        if (windowInSeconds < 1)
            _errors.Add($"{nameof(windowInSeconds)} must be greater than 0.  {windowInSeconds} was provided.");
        if (_errors.Any())
            throw new ConfigurationException(this, nameof(AddThrottling), _errors);

        Log.Configuration.ThrottleThreshold = threshold;
        Log.Configuration.ThrottleWindowInSeconds = windowInSeconds;
        Log.Configuration.Throttler = new()
        {
            Threshold = threshold,
            WindowInSeconds = windowInSeconds
        };
    });

    public LogConfigurationBuilder AddFlushing(int maxBufferSize, int intervalInSeconds, EventHandler<LogData[]> onFlush = null) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        if (maxBufferSize < 2)
            _errors.Add($"{nameof(maxBufferSize)} must be greater than 1.  {maxBufferSize} was provided.");
        if (intervalInSeconds < 1)
            _errors.Add($"{nameof(intervalInSeconds)} must be greater than 0.  {intervalInSeconds} was provided.");
        if (onFlush != null)
        {
            if (Log.Configuration.OnFlush != null)
                _errors.Add($"{nameof(AddFlushing)} has already been called and set {nameof(onFlush)}.");
            if (Log.Configuration.OnReroute != null)
                _errors.Add($"{nameof(RerouteIndividualLogs)} has already been called and set and is in conflict with {nameof(onFlush)}.");
        }

        if (_errors.Any())
            throw new ConfigurationException(this, nameof(AddFlushing), _errors);

        Log.Configuration.BufferSize = maxBufferSize;
        Log.Configuration.FlushIntervalInSeconds = intervalInSeconds;
        if (onFlush != null)
            Log.Configuration.OnFlush += onFlush;
    });

    public LogConfigurationBuilder SetMinimumSeverity(Severity severity) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        int[] valid = (int[])Enum.GetValues(typeof(Severity));
        if (!valid.Contains((int)severity))
            _errors.Add($"Provided log severity is not valid.");
        if (_errors.Any())
            throw new ConfigurationException(this, nameof(SetMinimumSeverity), _errors);

        Log.Configuration.MinimumSeverity = severity;
    });

    private LogConfigurationBuilder AssignOwners(Type type, int? defaultOwner) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        if (!type.IsEnum)
            _errors.Add("Owners Enum Type must be an enum.");

        Log.Configuration.OwnersEnumType = type;
        int[] values = Enum.GetValues(type).Cast<int>().ToArray();
        string[] names = Enum.GetNames(type);
        if (!values.Any())
            _errors.Add("Owners Enum Type must have at least one value.");
        defaultOwner ??= values.FirstOrDefault();
        if (values.All(value => value != defaultOwner))
            _errors.Add("Default owner must be a valid value in the provided enum.");
        if (_errors.Any())
            throw new ConfigurationException(this, nameof(AssignOwners), _errors);
        Log.Configuration.OwnerNames = values
            .Zip(names, (value, name) => new { value, name })
            .ToDictionary(x => x.value, x => x.name);
        Log.Configuration.DefaultOwner = (int)defaultOwner;
        Log.Configuration.Printing.LengthOwnerColumn = names.Max(name => name.Length);
    });

    public LogConfigurationBuilder AssignOwners(Type type) => AssignOwners(type, null);
    public LogConfigurationBuilder AssignOwners<T>(Type type, T defaultOwner) where T : Enum => AssignOwners(defaultOwner.GetType(), defaultOwner.AsInt());

    public LogConfigurationBuilder RerouteIndividualLogs(EventHandler<LogData> onLogSent) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        if (onLogSent == null)
            _errors.Add($"{nameof(onLogSent)} cannot be null.");
        if (Log.Configuration.OnReroute != null)
            _errors.Add($"{nameof(RerouteIndividualLogs)} has already been called and set.");
        if (Log.Configuration.OnFlush != null)
            _errors.Add($"{nameof(AddFlushing)} has already been called and set a log handler, and is in conflict with {nameof(onLogSent)}.");
        if (_errors.Any())
            throw new ConfigurationException(this, nameof(RerouteIndividualLogs), _errors);

        Log.Configuration.OnReroute += onLogSent;
    });

    public LogConfigurationBuilder DisableLineWrap() => OnceOnly<LogConfigurationBuilder>(() =>
    {
        if (Log.Configuration.DisableWrap)
            _errors.Add($"{nameof(DisableLineWrap)} has already been set.");
        if (_errors.Any())
            throw new ConfigurationException(this, nameof(DisableLineWrap), _errors);
        Log.Configuration.DisableWrap = true;
    });

    public LogConfigurationBuilder SetTimestampDisplay(TimestampDisplaySettings setting) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        setting.Validate();
        Log.Configuration.Printing.TimestampDisplaySetting = setting;
        Log.Configuration.Printing.LengthTimestampColumn = Log.Configuration.Printing.TimestampToString(TimestampMs.Now).Length + 6; // Timezone display takes 6 characters, e.g. "[UTC] ".
    });
    
    public LogConfigurationBuilder PrintExtras(Severity forSeverities, bool printData = true, bool printExceptions = true) => OnceOnly<LogConfigurationBuilder>(() =>
    {
        Log.Configuration.Printing.PrintData = printData;
        Log.Configuration.Printing.PrintExceptions = printExceptions;
    });
}