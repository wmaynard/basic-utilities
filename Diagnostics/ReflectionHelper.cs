using System.Reflection;

namespace Maynard.Diagnostics;

internal class ReflectionHelper
{
    internal static readonly string LibraryVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
    internal static readonly string ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
}