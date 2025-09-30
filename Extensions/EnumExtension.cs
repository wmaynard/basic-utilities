using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Maynard.ErrorHandling;

namespace Maynard.Extensions;

public static class EnumExtension
{
    // Validates that the enum value is a defined named constant of its enum type.
    // Throws ArgumentOutOfRangeException if the value is not defined.
    public static void Validate<T>(this T value) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new InternalException("Enum value is out of bounds.", ErrorCode.EnumOutOfBounds, data: new
            {
                Type = typeof(T).FullName
            });
    }
    
    public static string[] GetNames<T>() where T : struct, Enum => Enum.GetNames<T>();
    public static int[] GetValues<T>() where T : struct, Enum => Enum
        .GetValues<T>()
        .Select(value => (int)(object)value)
        .ToArray();
        
    /// <summary>
    /// Returns an array of individual enum values that are flagged.  This is useful for logging.
    /// </summary>
    public static T[] GetFlags<T>(this T flags) where T : Enum => flags
        .EnforceFlagAttribute()
        .All()
        .Where(t => flags.HasFlag(t))
        .ToArray();
    /// <summary>
    /// Throws an exception on anyone trying to use a normal, non-Flags enum for Flags-specific methods.
    /// </summary>
    private static T EnforceFlagAttribute<T>(this T obj) where T : Enum => obj.HasAttribute(out FlagsAttribute _) 
        ? obj 
        : throw new InternalException("Attempted to use a non-Flags enum for Flags-specific methods.", ErrorCode.EnumFlagsMisused);
    
    public static bool IsFlagOf<T>(this T obj, int value) where T : Enum
    {
        int asInt = (int)Convert.ChangeType(obj, typeof(int));

        return (asInt & value) == asInt;
    }
    
    /// <summary>
    /// When using an enum with the Flags attribute, this will return the opposite set of flags.
    /// </summary>
    public static T Invert<T>(this T flags) where T : Enum
    {
        flags.EnforceFlagAttribute();
    
        int inverted = ~flags.FullSet().AsInt() ^ ~flags.AsInt(); // Bitwise operations to flip the flags enum

        return inverted.AsEnum<T>();
    }
    
    /// <summary>
    /// Returns true if the current enum represents every possible flag.
    /// </summary>
    public static bool IsFullSet<T>(this T flags) where T : Enum => flags.AsInt() == flags.FullSet().AsInt();
    /// <summary>
    /// Returns an enum with every flag set.
    /// </summary>
    internal static T FullSet<T>(this T flags) where T : Enum
    {
        T[] all = flags.All();
        switch (all.Length)
        {
            case 0:
                return default;
            case 1:
                return all.First();
            default:
                int output = all.First().AsInt();
                return all
                    .Aggregate(output, (current, next) => current | next.AsInt())
                    .AsEnum<T>();
        }
    }
    public static int AsInt<T>(this T flags) where T : Enum => (int)(object)flags;
    private static T AsEnum<T>(this int _int) where T : Enum => (T)(object)_int;
    private static T[] All<T>(this T flags) where T : Enum => (T[])Enum.GetValues(flags.GetType());
    public static string GetDisplayName<T>(this T obj) where T : Enum
    {
        DisplayAttribute att = obj
            .GetType()
            .GetMember(obj.ToString())
            .First()
            .GetCustomAttribute<DisplayAttribute>();

        return att?.Name ?? obj.ToString();
    }
}