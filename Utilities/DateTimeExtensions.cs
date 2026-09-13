#nullable enable
using System.Globalization;

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Extension methods for DateTime objects.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Checks if the DateTime is in the past (compared to UtcNow).
    /// </summary>
    public static bool IsInPast(this DateTime dateTime)
    {
        return dateTime < DateTimeHelper.UtcNow;
    }

    /// <summary>
    /// Converts the DateTime to an ISO 8601 string (roundtrip format).
    /// </summary>
    public static string ToIsoString(this DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
    }
}