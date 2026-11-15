namespace Soenneker.Calcom.Blazor.Options;
/// <summary>Settings shared by inline, popup, and floating button embeds.</summary>
public class CalcomOptions
{
    /// <summary>A relative Cal.com booking path, such as username/intro or team/my-team/demo. Query parameters are allowed.</summary>
    public required string CalLink { get; set; }
    /// <summary>Prefills and booking parameters.</summary>
    public CalcomBookingConfig? Config { get; set; }
    /// <summary>Booking appearance and behavior.</summary>
    public CalcomUiOptions? Ui { get; set; }
}
