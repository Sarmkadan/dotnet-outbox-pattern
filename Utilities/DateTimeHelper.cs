// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.Utilities;

/// <summary>
/// Helper utilities for date/time operations throughout the outbox system
/// Provides consistent handling of UTC times and relative time calculations
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Gets the current UTC time - preferred over DateTime.Now for consistency
    /// </summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>
    /// Converts a Unix timestamp to a DateTime in UTC
    /// </summary>
    public static DateTime FromUnixTimestamp(long seconds)
    {
        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp
    /// </summary>
    public static long ToUnixTimestamp(DateTime dateTime)
    {
        return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    /// Calculates age in seconds relative to UtcNow
    /// </summary>
    public static long GetAgeSeconds(DateTime createdAt)
    {
        return (long)(UtcNow - createdAt).TotalSeconds;
    }

    /// <summary>
    /// Checks if a DateTime is older than specified days
    /// Used for archival and cleanup decisions
    /// </summary>
    public static bool IsOlderThan(DateTime dateTime, int days)
    {
        return dateTime < UtcNow.AddDays(-days);
    }

    /// <summary>
    /// Rounds a DateTime down to the nearest hour
    /// Useful for aggregating metrics
    /// </summary>
    public static DateTime RoundDownToHour(DateTime dateTime)
    {
        return dateTime.AddMinutes(-dateTime.Minute).AddSeconds(-dateTime.Second);
    }

    /// <summary>
    /// Rounds a DateTime down to the nearest day (midnight)
    /// </summary>
    public static DateTime RoundDownToDay(DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Gets the start of business hours (9 AM) for a given date
    /// </summary>
    public static DateTime GetBusinessHourStart(DateTime dateTime)
    {
        return dateTime.Date.AddHours(9);
    }

    /// <summary>
    /// Gets the end of business hours (5 PM) for a given date
    /// </summary>
    public static DateTime GetBusinessHourEnd(DateTime dateTime)
    {
        return dateTime.Date.AddHours(17);
    }

    /// <summary>
    /// Checks if a DateTime falls within business hours (9 AM - 5 PM)
    /// </summary>
    public static bool IsBusinessHours(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        var dayOfWeek = dateTime.DayOfWeek;

        // Exclude weekends
        if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
            return false;

        return hour >= 9 && hour < 17;
    }

    /// <summary>
    /// Parses a relative time period string (1h, 24h, 7d, 30d) to TimeSpan
    /// </summary>
    public static TimeSpan? ParseRelativePeriod(string? period)
    {
        if (string.IsNullOrEmpty(period))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(period, @"^(\d+)([hd])$");
        if (!match.Success)
            return null;

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value;

        return unit switch
        {
            "h" => TimeSpan.FromHours(value),
            "d" => TimeSpan.FromDays(value),
            _ => null
        };
    }

    /// <summary>
    /// Formats a duration in milliseconds as human-readable string
    /// </summary>
    public static string FormatDuration(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);

        if (ts.TotalSeconds < 1)
            return $"{milliseconds}ms";

        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F1}s";

        if (ts.TotalMinutes < 60)
            return $"{ts.TotalMinutes:F1}m";

        return $"{ts.TotalHours:F1}h";
    }

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "2 hours ago")
    /// </summary>
    public static string GetRelativeTimeString(DateTime dateTime)
    {
        var now = UtcNow;
        var span = now - dateTime;

        if (span.TotalSeconds < 60)
            return "just now";

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes != 1 ? "s" : "")} ago";

        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours} hour{((int)span.TotalHours != 1 ? "s" : "")} ago";

        if (span.TotalDays < 30)
            return $"{(int)span.TotalDays} day{((int)span.TotalDays != 1 ? "s" : "")} ago";

        return $"{(int)(span.TotalDays / 30)} month{((int)(span.TotalDays / 30) != 1 ? "s" : "")} ago";
    }
}
