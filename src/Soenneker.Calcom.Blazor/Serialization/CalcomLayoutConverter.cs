using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Soenneker.Calcom.Blazor.Enums;
namespace Soenneker.Calcom.Blazor.Serialization;
internal sealed class CalcomLayoutConverter : JsonConverter<CalcomLayout>
{
    public override CalcomLayout Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && CalcomLayout.TryFromValue(reader.GetString(), out var value) ? value : throw new JsonException("Unknown CalcomLayout.");
    public override void Write(Utf8JsonWriter writer, CalcomLayout value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
