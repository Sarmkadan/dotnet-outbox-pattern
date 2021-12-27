# DateTimeHelper

The `DateTimeHelper` class provides a collection of static utility methods for common date and time operations. It supports Unix timestamp conversion, age calculation, temporal rounding, business hours queries, and human-readable formatting of durations and relative times. All methods are stateless and designed for use in outbox pattern implementations where precise time handling is required.

## API

### `FromUnixTimestamp`
```csharp
public static DateTime FromUnixTimestamp(long timestamp)
```
Converts a Unix timestamp (seconds elapsed since 1970-01-01 00:00:00 UTC) to a `DateTime` in UTC.  
**Parameters:**  
- `timestamp` – The number of seconds since the Unix epoch.  
**Returns:** A `DateTime` representing the corresponding UTC moment.  
**Throws:** `ArgumentOutOfRangeException` if `timestamp` is less than `DateTime.UnixEpoch.Ticks / TimeSpan.TicksPerSecond` (i.e., before the epoch) or if the resulting `DateTime` would overflow.

### `ToUnixTimestamp`
```csharp
public static long ToUnixTimestamp(DateTime dateTime)
```
Converts a `DateTime` to the number of seconds since the Unix epoch.  
**Parameters:**  
- `dateTime` – The date and time to convert. Its `Kind` is assumed to be UTC; if not, the conversion may produce unexpected results.  
**Returns:** A `long` representing the Unix timestamp.  
**Throws:** `ArgumentOutOfRangeException` if `dateTime` is before `DateTime.UnixEpoch`.

### `GetAgeSeconds`
```csharp
public static long GetAgeSeconds(DateTime dateTime)
```
Calculates the number of seconds elapsed from `dateTime` to the current UTC time (`DateTime.UtcNow`).  
**Parameters:**  
- `dateTime` – The starting point in time (typically a past moment).  
**Returns:** A `long` representing the age in seconds. If `dateTime` is in the future, the result is negative.  
**Throws:** None.

### `IsOlderThan`
```csharp
public static bool IsOlderThan(DateTime dateTime, TimeSpan threshold)
```
Determines whether the age of `dateTime` (relative to `DateTime.UtcNow`) exceeds the specified `threshold`.  
**Parameters:**  
- `dateTime` – The timestamp to evaluate.  
- `threshold` – The minimum age to compare against.  
**Returns:** `true` if `(DateTime.UtcNow - dateTime) > threshold`; otherwise `false`.  
**Throws:** None.

### `RoundDownToHour`
```csharp
public static DateTime RoundDownToHour(DateTime dateTime)
```
Rounds `dateTime` down to the start of the hour (minutes, seconds, and milliseconds set to zero). The `Kind` of the input is preserved.  
**Parameters:**  
- `dateTime` – The input date and time.  
**Returns:** A `DateTime` representing the beginning of the same hour.  
**Throws:** None.

### `RoundDownToDay`
```csharp
public static DateTime RoundDownToDay(DateTime dateTime)
```
Rounds `dateTime` down to the start of the day (time component set to 00:00:00.000). The `Kind` of the input is preserved.  
**Parameters:**  
- `dateTime` – The input date and time.  
**Returns:** A `DateTime` representing the beginning of the same day.  
**Throws:** None.

### `GetBusinessHourStart`
```csharp
public static DateTime GetBusinessHourStart(DateTime date)
```
Returns the start of business hours for the given date. The implementation defines business hours as 09:00:00 local time (the time zone is not adjusted; the `Kind` of the input is preserved).  
**Parameters:**  
- `date` – The date for which to retrieve the business hour start.  
**Returns:** A `DateTime` with the same date and time set to 09:00:00.  
**Throws:** None.

### `GetBusinessHourEnd`
```csharp
public static DateTime GetBusinessHourEnd(DateTime date)
```
Returns the end of business hours for the given date. The implementation defines business hours as 17:00:00 local time.  
**Parameters:**  
- `date` – The date for which to retrieve the business hour end.  
**Returns:** A `DateTime` with the same date and time set to 17:00:00.  
**Throws:** None.

### `IsBusinessHours`
```csharp
public static bool IsBusinessHours(DateTime dateTime)
```
Checks whether the given `dateTime` falls within business hours (09:00–17:00) on its date. The time component is compared against the fixed start and end times; the `Kind` is not converted.  
**Parameters:**  
- `dateTime` – The moment to evaluate.  
**Returns:** `true` if the time of day is between 09:00 (inclusive) and 17:00 (exclusive); otherwise `false`.  
**Throws:** None.

### `ParseRelativePeriod`
```csharp
public static TimeSpan? ParseRelativePeriod(string input)
```
Attempts to parse a relative period string into a `TimeSpan`. Supported formats include single‑unit expressions such as `"5m"` (minutes), `"2h"` (hours), `"1d"` (days), and `"30s"` (seconds). Multiple units are not supported.  
**Parameters:**  
- `input` – The string to parse (e.g., `"10m"`, `"3h"`).  
**Returns:** A `TimeSpan` if parsing succeeds; `null` if the input is null, empty, or does not match the expected pattern.  
**Throws:** None.

### `FormatDuration`
```csharp
public static string FormatDuration(TimeSpan duration)
```
Formats a `TimeSpan` into a human‑readable string, e.g., `"2 hours 30 minutes"`. Zero components are omitted. If the duration is zero, the result is `"0 seconds"`.  
**Parameters:**  
- `duration` – The time span to format.  
**Returns:** A string representation of the duration.  
**Throws:** None.

### `GetRelativeTimeString`
```csharp
public static string GetRelativeTimeString(DateTime dateTime)
```
Returns a human‑readable relative time string describing the difference between `dateTime` and the current UTC time. Examples: `"5 minutes ago"`, `"in 2 hours"`, `"just now"`.  
**Parameters:**  
- `dateTime` – The timestamp to compare against `DateTime.UtcNow`.  
**Returns:** A string such as `"3 days ago"` or `"in 1 hour"`.  
**Throws:** None.

## Usage

### Example 1: Unix timestamp conversion and age check
```csharp
long unixTimestamp = 1700000000;
DateTime utcTime = DateTimeHelper.FromUnixTimestamp(unixTimestamp);

if (DateTimeHelper.IsOlderThan(utcTime, TimeSpan.FromHours(1)))
{
    Console.WriteLine("The timestamp is more than 1 hour old.");
}

long ageSeconds = DateTimeHelper.GetAgeSeconds(utcTime);
Console.WriteLine($"Age in seconds: {ageSeconds}");
```

### Example 2: Rounding and business hours
```csharp
DateTime now = DateTime.UtcNow;
DateTime startOfDay = DateTimeHelper.RoundDownToDay(now);
DateTime businessStart = DateTimeHelper.GetBusinessHourStart(startOfDay);
DateTime businessEnd = DateTimeHelper.GetBusinessHourEnd(startOfDay);

if (DateTimeHelper.IsBusinessHours(now))
{
    TimeSpan remaining = businessEnd - now;
    Console.WriteLine($"Business hours active. Remaining: {DateTimeHelper.FormatDuration(remaining)}");
}
else
{
    Console.WriteLine("Outside business hours.");
}

// Parse a relative period
TimeSpan? period = DateTimeHelper.ParseRelativePeriod("2h");
if (period.HasValue)
{
    Console.WriteLine($"Parsed period: {DateTimeHelper.FormatDuration(period.Value)}");
}
```

## Notes

- **Edge cases:**  
  - `FromUnixTimestamp` throws for timestamps before the Unix epoch (negative values) or values that would overflow the `DateTime` range.  
  - `GetAgeSeconds` and `IsOlderThan` accept future dates; `GetAgeSeconds` returns a negative number, and `IsOlderThan` returns `false` for any future date (since the age is negative).  
  - `ParseRelativePeriod` returns `null` for invalid input; it does not throw.  
  - Business hours are hard‑coded to 09:00–17:00 local time. The methods do not perform time zone conversion; the `Kind` of the input `DateTime` is preserved.  
- **Thread safety:** All members are static and do not access any shared mutable state. The class is inherently thread‑safe.  
- **Time zone assumptions:** Methods that compare against the current time (`GetAgeSeconds`, `IsOlderThan`, `GetRelativeTimeString`) use `DateTime.UtcNow`. Methods that round or extract business hours preserve the `Kind` of the input but do not adjust for time zone offsets.
