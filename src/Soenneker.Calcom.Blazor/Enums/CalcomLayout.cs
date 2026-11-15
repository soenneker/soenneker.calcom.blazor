using Soenneker.Gen.EnumValues;
namespace Soenneker.Calcom.Blazor.Enums;
/// <summary>Booking calendar layouts.</summary>
[EnumValue<string>]
public sealed partial class CalcomLayout
{
    private CalcomLayout() => throw new System.NotSupportedException("Use a declared value.");
    /// <summary>MonthView.</summary>
    public static readonly CalcomLayout MonthView = new("month_view");
    /// <summary>WeekView.</summary>
    public static readonly CalcomLayout WeekView = new("week_view");
    /// <summary>ColumnView.</summary>
    public static readonly CalcomLayout ColumnView = new("column_view");
}
