using Soenneker.Gen.EnumValues;
namespace Soenneker.Calcom.Blazor.Enums;
/// <summary>Booking interface themes.</summary>
[EnumValue<string>]
public sealed partial class CalcomTheme
{
    private CalcomTheme() => throw new System.NotSupportedException("Use a declared value.");
    /// <summary>Auto.</summary>
    public static readonly CalcomTheme Auto = new("auto");
    /// <summary>Light.</summary>
    public static readonly CalcomTheme Light = new("light");
    /// <summary>Dark.</summary>
    public static readonly CalcomTheme Dark = new("dark");
}
