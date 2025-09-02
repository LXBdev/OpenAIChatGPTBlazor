using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAIChatGPTBlazor.Services.Models;

public class VideoGenerationRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 480;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 480;

    [JsonPropertyName("n_seconds")]
    public int NSeconds { get; set; } = 5;

    [JsonPropertyName("model")]
    public string Model { get; set; } = "sora";
}

public class VideoJobResponse
{
    [JsonPropertyName("id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableUnixTimestampConverter))]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("error")]
    public VideoError? Error { get; set; }
}

public class VideoError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class VideoGeneration
{
    [JsonPropertyName("id")]
    public string GenerationId { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; } = "mp4";

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;
}

public class VideoJobStatusResponse
{
    [JsonPropertyName("id")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(UnixTimestampConverter))]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("expires_at")]
    [JsonConverter(typeof(NullableUnixTimestampConverter))]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("generations")]
    public List<VideoGeneration>? Generations { get; set; }

    [JsonPropertyName("error")]
    public VideoError? Error { get; set; }
}

public enum VideoJobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
}

public static class VideoResolutions
{
    public static readonly Dictionary<string, string> Options = new()
    {
        { "1920x1080", "Full HD (1920x1080)" },
        { "1080x1920", "Vertical HD (1080x1920)" },
        { "1280x720", "HD (1280x720)" },
        { "720x1280", "Vertical HD (720x1280)" },
        { "1024x1024", "Square (1024x1024)" },
    };
}

// Unix timestamp JSON converter for DateTime
public class UnixTimestampConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle Unix timestamp (seconds since epoch)
            var unixTimestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            // Handle ISO 8601 string format as fallback
            var dateString = reader.GetString();
            if (
                DateTime.TryParse(
                    dateString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate
                )
            )
            {
                return parsedDate;
            }
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to DateTime");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Write as Unix timestamp
        var unixTimestamp = ((DateTimeOffset)value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unixTimestamp);
    }
}

// Nullable Unix timestamp JSON converter
public class NullableUnixTimestampConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var unixTimestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (
                DateTime.TryParse(
                    dateString,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedDate
                )
            )
            {
                return parsedDate;
            }
        }

        throw new JsonException($"Unable to convert {reader.TokenType} to DateTime?");
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime? value,
        JsonSerializerOptions options
    )
    {
        if (value.HasValue)
        {
            var unixTimestamp = ((DateTimeOffset)value.Value).ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTimestamp);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public static class VideoDurations
{
    public static readonly int[] Options = { 5, 10, 15, 20 };
}

public static class VideoStyles
{
    public static readonly Dictionary<string, string> Options = new()
    {
        { "", "Default" },
        { "cinematic", "Cinematic" },
        { "photorealistic", "Photorealistic" },
        { "animated", "Animated" },
        { "artistic", "Artistic" },
        { "documentary", "Documentary" },
    };
}
