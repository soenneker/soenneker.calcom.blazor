namespace Soenneker.Calcom.Blazor.Configuration;
/// <summary>Cal.com instance settings. Configure once per service scope; a page can load only one embed SDK.</summary>
public sealed class CalcomConfiguration
{
    /// <summary>The HTTPS origin of the booking application, or an HTTP loopback origin for local development.</summary>
    public string Origin { get; set; } = "https://app.cal.com";
    /// <summary>The HTTPS embed SDK URL. Self-hosted installations typically use their origin plus /embed/embed.js.</summary>
    public string EmbedScriptUrl { get; set; } = "https://app.cal.com/embed/embed.js";
}
