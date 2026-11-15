using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Calcom.Blazor.Options;
using Soenneker.Calcom.Blazor.Dtos;
namespace Soenneker.Calcom.Blazor.Abstract;
/// <summary>Scoped Cal.com embed integration. Invoke after interactive rendering. Unsubscribe event handlers when consumers are disposed.</summary>
public interface ICalcom : IAsyncDisposable
{
    /// <summary>Raised for bookingSuccessfulV2 after a booking is created. Use server webhooks for authoritative confirmation.</summary>
    event Func<CalcomEvent, Task>? BookingSuccessful;
    /// <summary>Raised when an event type is selected.</summary>
    event Func<CalcomEvent, Task>? EventTypeSelected;
    /// <summary>Raised when an embed is ready.</summary>
    event Func<CalcomEvent, Task>? LinkReady;
    /// <summary>Raised when an embed fails to load. Data contains the SDK error details.</summary>
    event Func<CalcomEvent, Task>? LinkFailed;
    /// <summary>Raised when available slots are ready for interaction.</summary>
    event Func<CalcomEvent, Task>? BookerReady;
    /// <summary>Loads the SDK and JavaScript wrapper without displaying a widget.</summary>
    ValueTask Initialize(CancellationToken cancellationToken = default);
    /// <summary>Creates or replaces an inline widget in an empty, dedicated container whose dimensions are set by the caller.</summary>
    ValueTask CreateInline(string elementId, CalcomOptions options, CancellationToken cancellationToken = default);
    /// <summary>Removes an inline widget owned by this service without removing its container.</summary>
    ValueTask DestroyInline(string elementId, CancellationToken cancellationToken = default);
    /// <summary>Opens a scheduling popup. Each service owns its modal namespace.</summary>
    ValueTask OpenPopup(CalcomOptions options, CancellationToken cancellationToken = default);
    /// <summary>Closes and removes this service's popup.</summary>
    ValueTask ClosePopup(CancellationToken cancellationToken = default);
    /// <summary>Creates or replaces this service's floating scheduling button.</summary>
    ValueTask CreateFloatingButton(CalcomFloatingButtonOptions options, CancellationToken cancellationToken = default);
    /// <summary>Removes this service's floating button and its modal.</summary>
    ValueTask DestroyFloatingButton(CancellationToken cancellationToken = default);
    /// <summary>Updates the UI of an existing inline widget.</summary>
    ValueTask UpdateInlineUi(string elementId, CalcomUiOptions options, CancellationToken cancellationToken = default);
}
