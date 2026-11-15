using Soenneker.Gen.EnumValues;
namespace Soenneker.Calcom.Blazor.Enums;
/// <summary>Floating button positions.</summary>
[EnumValue<string>]
public sealed partial class CalcomButtonPosition
{
    private CalcomButtonPosition() => throw new System.NotSupportedException("Use a declared value.");
    /// <summary>BottomLeft.</summary>
    public static readonly CalcomButtonPosition BottomLeft = new("bottom-left");
    /// <summary>BottomRight.</summary>
    public static readonly CalcomButtonPosition BottomRight = new("bottom-right");
}
