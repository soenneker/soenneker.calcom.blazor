using Soenneker.Calcom.Blazor.Enums;
namespace Soenneker.Calcom.Blazor.Options;
/// <summary>Appearance of the SDK's floating booking button.</summary>
public sealed class CalcomFloatingButtonOptions : CalcomOptions
{
    /// <summary>Visible button text.</summary>
    public string ButtonText { get; set; } = "Book a demo";
    /// <summary>Hides the calendar icon.</summary>
    public bool HideButtonIcon { get; set; }
    /// <summary>Button placement.</summary>
    public CalcomButtonPosition ButtonPosition { get; set; } = CalcomButtonPosition.BottomRight;
    /// <summary>Button background color.</summary>
    public string ButtonColor { get; set; } = "#000000";
    /// <summary>Button text color.</summary>
    public string ButtonTextColor { get; set; } = "#ffffff";
}
