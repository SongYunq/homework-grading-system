using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeworkGrading.Api.Http;

internal sealed class OffsetDateTimeConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.GetString() is { } value &&
            (value.EndsWith('Z') || (value.Length > 16 && value[^6] is '+' or '-' && value[^3] == ':')) &&
            reader.TryGetDateTimeOffset(out var timestamp))
            return timestamp.ToUniversalTime();

        throw new JsonException("An ISO 8601 date-time with an explicit offset is required.");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime());
}
