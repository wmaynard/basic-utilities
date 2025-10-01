namespace Maynard.Configuration;

public class ConfigurationException(object sender, string methodName, List<string> errors) : Exception($"Invalid configuration options! [{sender.GetType().Name} > {methodName}]")
{
    public string[] Errors { get; set; } = errors.ToArray();
}