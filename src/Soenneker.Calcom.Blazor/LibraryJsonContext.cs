using System.Text.Json.Serialization;
using Soenneker.Calcom.Blazor.Configuration;
using Soenneker.Calcom.Blazor.Options;
using Soenneker.Calcom.Blazor.Dtos;
using Soenneker.Calcom.Blazor.Serialization;
namespace Soenneker.Calcom.Blazor;
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = new[] { typeof(CalcomThemeConverter), typeof(CalcomLayoutConverter), typeof(CalcomButtonPositionConverter) })]
[JsonSerializable(typeof(CalcomConfiguration))]
[JsonSerializable(typeof(CalcomOptions))]
[JsonSerializable(typeof(CalcomFloatingButtonOptions))]
[JsonSerializable(typeof(CalcomUiOptions))]
[JsonSerializable(typeof(CalcomEvent))]
internal partial class LibraryJsonContext : JsonSerializerContext;
