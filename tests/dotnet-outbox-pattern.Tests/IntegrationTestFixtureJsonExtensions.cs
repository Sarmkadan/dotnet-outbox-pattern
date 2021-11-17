#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetOutboxPattern.Tests;

public static class IntegrationTestFixtureJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson(this IntegrationTestFixture value, bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    public static IntegrationTestFixture? FromJson(string json)
    {
        return JsonSerializer.Deserialize<IntegrationTestFixture>(json, _jsonSerializerOptions);
    }

    public static bool TryFromJson(string json, out IntegrationTestFixture? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<IntegrationTestFixture>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}