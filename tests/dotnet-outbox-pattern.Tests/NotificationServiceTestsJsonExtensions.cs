#nullable enable

using System.Text.Json;

namespace DotnetOutboxPattern.Tests;

public static class NotificationServiceTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string ToJson(this NotificationServiceTests value, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    public static NotificationServiceTests? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<NotificationServiceTests>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryFromJson(string json, out NotificationServiceTests? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<NotificationServiceTests>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}