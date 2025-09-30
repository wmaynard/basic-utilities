using System.Diagnostics;

namespace Maynard.Diagnostics;

public static class Breakpoint
{
    [Obsolete("This is for debugging only.  While the code involved should be optimized out on a release build, leaving these method calls committed is bad practice.")]
    public static void Stop()
    {
        #if DEBUG
        Debugger.Break();
        #endif
    }
    
    [Obsolete("This is for debugging only.  While the code involved should be optimized out on a release build, leaving these method calls committed is bad practice.")]
    public static void When(bool condition)
    {
        #if DEBUG
        if (condition)
            Debugger.Break();
        #endif
    }
}