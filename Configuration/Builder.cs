using System.Runtime.CompilerServices;
using Maynard.Logging;

namespace Maynard.Configuration;

public abstract class Builder
{
    protected HashSet<string> CalledMethods { get; set; } = new();

    protected T OnceOnly<T>(Action action, [CallerMemberName] string caller = null) where T : Builder
    {
        if (!string.IsNullOrWhiteSpace(caller) && !CalledMethods.Add(caller))
        {
            Log.Critical("Configuration builder method already called!  Each method can only be called once.  Subsequent calls are ignored.", new
            {
                Method = caller
            }).Wait();
            return (T)this;
        }
        action.Invoke();
        return (T)this;
    }
}