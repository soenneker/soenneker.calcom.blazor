using System.Text.Json;
namespace Soenneker.Calcom.Blazor.Dtos;
/// <summary>A notification from an owned Cal.com widget. Browser events are not authoritative server-side booking confirmation.</summary>
public sealed class CalcomEvent
{
    /// <summary>The inline container ID, or null for a modal-based embed.</summary>
    public string? ElementId { get; set; }
    /// <summary>The unique SDK namespace assigned to the widget.</summary>
    public string Namespace { get; set; } = "";
    /// <summary>The original event data supplied by Cal.com.</summary>
    public JsonElement Data { get; set; }
    /// <summary>Booking UID supplied by bookingSuccessfulV2, if present.</summary>
    public string? BookingUid => Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("uid", out var uid) && uid.ValueKind == JsonValueKind.String ? uid.GetString() : null;
}
