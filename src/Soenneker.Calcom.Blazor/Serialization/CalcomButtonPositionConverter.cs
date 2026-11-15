using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Soenneker.Calcom.Blazor.Enums;
namespace Soenneker.Calcom.Blazor.Serialization;
internal sealed class CalcomButtonPositionConverter : JsonConverter<CalcomButtonPosition>
{
    public override CalcomButtonPosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && CalcomButtonPosition.TryFromValue(reader.GetString(), out var value) ? value : throw new JsonException("Unknown CalcomButtonPosition.");
    public override void Write(Utf8JsonWriter writer, CalcomButtonPosition value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
