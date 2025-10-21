namespace Maynard.Extensions;

public static class NumericExtension
{
    /// <summary>
    /// Tests if the value is between the min and max values, inclusive.
    /// </summary>
    /// <param name="value">The integer to test.</param>
    /// <param name="min">The minimum accepted value.</param>
    /// <param name="max">The maximum accepted value.</param>
    /// <returns>True if the value lies within the specified range.</returns>
    public static bool IsBetween(this int value, int min, int max) => value >= min && value <= max;

    /// <summary>
    /// Tests if the value is not between the min and max values, inclusive.
    /// </summary>
    /// <param name="value">The integer to test.</param>
    /// <param name="min">The minimum rejected value.</param>
    /// <param name="max">The maximum rejected value.</param>
    /// <returns>True if the value does not lie within the specified range.</returns>
    public static bool IsNotBetween(this int value, int min, int max) => !value.IsBetween(min, max);
    // TODO: extend to other numeric types.
    
}