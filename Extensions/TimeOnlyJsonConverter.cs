using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace Darb.Api.Extensions;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private readonly string _format;

    public TimeOnlyJsonConverter() : this("HH:mm")
    {
    }

    public TimeOnlyJsonConverter(string format)
    {
        _format = format;
    }

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        // Try parsing with common formats
        string[] formats = { "HH:mm", "HH:mm:ss", "h:mm tt", "hh:mm tt", "H:mm", "HH:mm:ss.fff", "HH:mm:ss.fffffff" };
        
        if (TimeOnly.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time;
        }

        // Try parsing as a full ISO DateTime string (common in JS)
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return TimeOnly.FromDateTime(dateTime);
        }

        // Final fallback to standard TryParse
        if (TimeOnly.TryParse(value, out time))
        {
            return time;
        }

        throw new JsonException($"Unable to convert \"{value}\" to TimeOnly. Expected formats: HH:mm, HH:mm:ss, or ISO 8601 time string.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(_format, CultureInfo.InvariantCulture));
    }
}
