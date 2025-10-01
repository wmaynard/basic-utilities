namespace Maynard.Singletons;

using Maynard.Json;

public interface ISingleton
{
    public string Name { get; }
    public FlexJson HealthStatus { get; }
}