namespace Maynard.Logging;

[Flags]
public enum Severity
{
    Verbose = 0b_0000_0001,
    Info = 0b_0000_0010,
    Warn = 0b_0000_0100,
    Error = 0b_0000_1000,
    Alert = 0b_0001_0000,
    Good = 0b_0010_0000,
}