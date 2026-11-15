[![](https://img.shields.io/nuget/v/soenneker.calcom.blazor.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.calcom.blazor/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.calcom.blazor/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.calcom.blazor/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.calcom.blazor.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.calcom.blazor/)
[![](https://img.shields.io/badge/Demo-Live-blueviolet?style=for-the-badge&logo=github)](https://soenneker.github.io/soenneker.calcom.blazor)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.calcom.blazor/codeql.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.calcom.blazor/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Calcom.Blazor
### A Blazor interop library for Cal.com scheduling embeds

## Installation

```bash
dotnet add package Soenneker.Calcom.Blazor
```

## Setup

```csharp
using Soenneker.Calcom.Blazor.Registrars;

builder.Services.AddCalcomAsScoped();
```

Resources load lazily on first use. No API key or script tags are required. Call from an interactive component after rendering, not during prerendering. Your content security policy must allow the Cal.com SDK, its styles, and booking frames.

For self-hosted Cal.com:

```csharp
builder.Services.AddCalcomAsScoped(options =>
{
    options.Origin = "https://booking.example.com";
    options.EmbedScriptUrl = "https://booking.example.com/embed/embed.js";
});
```

Only one embed SDK URL can be loaded per browser page. HTTPS is required except for HTTP loopback development. `Origin` is the booking application's origin, not a URL path.

## Popup

```razor
@using Soenneker.Calcom.Blazor.Abstract
@using Soenneker.Calcom.Blazor.Enums
@inject ICalcom Calcom

<button @onclick="BookDemo">Book a demo</button>

@code {
    private async Task BookDemo() => await Calcom.OpenPopup(new()
    {
        CalLink = "your-name/demo",
        Config = new() { Name = "Jane Doe", Email = "jane@example.com", UtmSource = "website" },
        Ui = new() { Theme = CalcomTheme.Auto, Layout = CalcomLayout.MonthView }
    });
}
```

Use a relative `CalLink`, such as `your-name/demo` or `team/your-team/demo`, optionally followed by query parameters. Use `ClosePopup()` when navigating away or closing your flow.

## Inline component

```razor
@using Soenneker.Calcom.Blazor.Components
@using Soenneker.Calcom.Blazor.Options
@using Soenneker.Calcom.Blazor.Dtos

<CalcomInline Options="_options" OnBookingSuccessful="Booked" />

@code {
    private readonly CalcomOptions _options = new() { CalLink = "your-name/demo" };

    private Task Booked(CalcomEvent booking)
    {
        // booking.BookingUid identifies the booking when supplied by Cal.com.
        return Task.CompletedTask;
    }
}
```

The component creates its widget after rendering, refreshes when options change, and removes its widget when disposed. `Style` sets its container dimensions (default minimum width 320px and height 750px). Its generated HTML ID is reserved.

For manual control, call `CreateInline(elementId, options)` after rendering an empty, dedicated container. Use `UpdateInlineUi(elementId, options)` to adjust appearance without recreating it, and `DestroyInline(elementId)` when finished.

## Floating button

```csharp
await Calcom.CreateFloatingButton(new()
{
    CalLink = "your-name/demo",
    ButtonText = "Book a demo",
    ButtonPosition = CalcomButtonPosition.BottomRight,
    ButtonColor = "#18181b",
    ButtonTextColor = "#ffffff",
    HideButtonIcon = true
});

await Calcom.DestroyFloatingButton();
```

Each service owns one popup and one floating button. Inline widgets, popups, and floating buttons receive separate SDK namespaces. Removing one service's widget leaves other services' widgets intact.

## Appearance and prefills

`CalcomUiOptions` provides typed theme (`Auto`, `Light`, `Dark`) and layout (`MonthView`, `WeekView`, `ColumnView`) values, event detail visibility, automatic scrolling, mobile slot behavior, and per-theme CSS variable dictionaries. CSS variable names follow Cal.com's embed configuration.

`CalcomBookingConfig` supports name, email, guests, notes, locale, time zone, and UTM tracking. `AdditionalFields` accepts custom booking answers and query parameters. Typed values take precedence; internal SDK parameters such as `iframeAttrs` are reserved.

## Events and lifecycle

Subscribe to the asynchronous service events `BookingSuccessful`, `EventTypeSelected`, `LinkReady`, `LinkFailed`, and `BookerReady`. `BookingSuccessful` uses Cal.com's current `bookingSuccessfulV2` payload. Each `CalcomEvent` contains the SDK namespace, original `Data`, and the container `ElementId` for inline widgets (null for modal-based widgets).

The callback bridge validates the origin, namespace, and sending iframe. Browser notifications are useful for UI and analytics; use server-side webhooks for authoritative booking confirmation. Service event subscribers should marshal UI changes through their component's `InvokeAsync` and unsubscribe when disposed. The inline component's callback already dispatches and filters to its widget.

Remove your widgets on navigation without disposing the shared scoped service. Service disposal removes owned DOM, callback references, and the wrapper's message listener. Cal.com's SDK has no public namespace destructor; idle namespaces are reused by origin and widget kind, while its shared global SDK infrastructure stays loaded.

## Demo and verification

The demo accepts your booking path and supports themes, layouts, prefills, all three embed types, widget removal, and readiness/failure/booking notifications. It never books an appointment automatically.

```shell
dotnet run --project test/Soenneker.Calcom.Blazor.Demo
dotnet test --project test/Soenneker.Calcom.Blazor.Tests -- --treenode-filter "/*/*/CalcomTests/*"
node --test test/Soenneker.Calcom.Blazor.Tests/calcominterop.test.mjs
```
