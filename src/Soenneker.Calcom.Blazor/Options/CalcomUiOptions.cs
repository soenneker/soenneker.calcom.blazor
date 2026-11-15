using System.Collections.Generic;
using Soenneker.Calcom.Blazor.Enums;
namespace Soenneker.Calcom.Blazor.Options;
/// <summary>Supported Cal.com UI controls, applied within the widget's namespace.</summary>
public sealed class CalcomUiOptions
{
    /// <summary>Color theme.</summary>
    public CalcomTheme Theme { get; set; } = CalcomTheme.Auto;
    /// <summary>Calendar layout.</summary>
    public CalcomLayout Layout { get; set; } = CalcomLayout.MonthView;
    /// <summary>Hides the event details sidebar.</summary>
    public bool HideEventTypeDetails { get; set; }
    /// <summary>Disables automatic scrolling.</summary>
    public bool DisableAutoScroll { get; set; }
    /// <summary>Uses the slots view on small screens.</summary>
    public bool UseSlotsViewOnSmallScreen { get; set; }
    /// <summary>Cal.com CSS custom property overrides for the light theme.</summary>
    public Dictionary<string, string>? LightCssVariables { get; set; }
    /// <summary>Cal.com CSS custom property overrides for the dark theme.</summary>
    public Dictionary<string, string>? DarkCssVariables { get; set; }
}
