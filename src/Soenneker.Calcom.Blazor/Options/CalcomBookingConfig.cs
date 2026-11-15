using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Soenneker.Calcom.Blazor.Options;
/// <summary>Booking form prefills and tracking parameters.</summary>
public sealed class CalcomBookingConfig
{
    /// <summary>Invitee name.</summary>
    public string? Name { get; set; }
    /// <summary>Invitee email address.</summary>
    public string? Email { get; set; }
    /// <summary>Additional guest email addresses.</summary>
    public List<string>? Guests { get; set; }
    /// <summary>Booking notes.</summary>
    public string? Notes { get; set; }
    /// <summary>Booker language, for example en.</summary>
    public string? Locale { get; set; }
    /// <summary>IANA time zone, for example America/Chicago.</summary>
    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }
    /// <summary>Campaign source.</summary>
    [JsonPropertyName("utm_source")]
    public string? UtmSource { get; set; }
    /// <summary>Campaign medium.</summary>
    [JsonPropertyName("utm_medium")]
    public string? UtmMedium { get; set; }
    /// <summary>Campaign name.</summary>
    [JsonPropertyName("utm_campaign")]
    public string? UtmCampaign { get; set; }
    /// <summary>Campaign term.</summary>
    [JsonPropertyName("utm_term")]
    public string? UtmTerm { get; set; }
    /// <summary>Campaign content.</summary>
    [JsonPropertyName("utm_content")]
    public string? UtmContent { get; set; }
    /// <summary>Additional query parameters and custom booking answers. Typed properties take precedence; SDK internal keys are reserved.</summary>
    public Dictionary<string, string>? AdditionalFields { get; set; }
}
