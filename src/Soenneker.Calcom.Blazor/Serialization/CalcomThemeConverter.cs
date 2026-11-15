using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Soenneker.Calcom.Blazor.Enums;
namespace Soenneker.Calcom.Blazor.Serialization;
internal sealed class CalcomThemeConverter : JsonConverter<CalcomTheme>
{
    public override CalcomTheme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && CalcomTheme.TryFromValue(reader.GetString(), out var value) ? value : throw new JsonException("Unknown CalcomTheme.");
    public override void Write(Utf8JsonWriter writer, CalcomTheme value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
