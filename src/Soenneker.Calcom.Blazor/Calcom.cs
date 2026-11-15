using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Soenneker.Asyncs.Initializers;
using Soenneker.Blazor.Utils.ResourceLoader.Abstract;
using Soenneker.Calcom.Blazor.Abstract;
using Soenneker.Calcom.Blazor.Dtos;
using Soenneker.Calcom.Blazor.Options;
using Soenneker.Calcom.Blazor.Configuration;
namespace Soenneker.Calcom.Blazor;
public sealed class Calcom : ICalcom
{
    private readonly CalcomConfiguration _configuration;
    private readonly string _id = Guid.NewGuid().ToString();
    private readonly IJSRuntime _jsRuntime;
    private readonly IResourceLoader _resourceLoader;
    private readonly AsyncInitializer _initializer;
    private readonly CancellationTokenSource _lifetime = new();
    private IJSObjectReference? _module;
    private IJSObjectReference? _session;
    private DotNetObjectReference<Calcom>? _reference;
    private bool _disposed;
    public event Func<CalcomEvent, Task>? LinkReady;
    public event Func<CalcomEvent, Task>? EventTypeSelected;
    public event Func<CalcomEvent, Task>? LinkFailed;
    public event Func<CalcomEvent, Task>? BookingSuccessful;

    public event Func<CalcomEvent, Task>? BookerReady;

    public Calcom(IJSRuntime jsRuntime, IResourceLoader resourceLoader, CalcomConfiguration configuration)
    {
        ValidateConfiguration(configuration);
        _configuration = new() { Origin = configuration.Origin.TrimEnd('/'), EmbedScriptUrl = configuration.EmbedScriptUrl };
        _jsRuntime = jsRuntime;
        _resourceLoader = resourceLoader;
        _initializer = new AsyncInitializer(Load);
    }

    private async ValueTask Load(CancellationToken token)
    {
        try
        {
            _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./_content/Soenneker.Calcom.Blazor/js/calcominterop.js");
            await _module.InvokeVoidAsync("prepare", token, _configuration.EmbedScriptUrl);
            await _resourceLoader.LoadScriptAndWaitForVariable(_configuration.EmbedScriptUrl, "Cal.instance", crossOrigin: null, cancellationToken: token);
            _reference ??= DotNetObjectReference.Create(this);
            _session ??= await _module.InvokeAsync<IJSObjectReference>("create", token, _id,
                JsonSerializer.Serialize(_configuration, LibraryJsonContext.Default.CalcomConfiguration), _reference);
        }
        catch (JSDisconnectedException) { throw; }
        catch (JSException ex) { throw new InvalidOperationException("Unable to initialize the Cal.com embed SDK.", ex); }
    }

    public async ValueTask Initialize(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _initializer.Init(linked.Token);
    }

    private async ValueTask Invoke(string method, CancellationToken token, params object?[] args)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
        await _initializer.Init(linked.Token);
        try { await _session!.InvokeVoidAsync(method, linked.Token, args); }
        catch (JSDisconnectedException) { throw; }
        catch (JSException ex) { throw new InvalidOperationException($"Calcom operation '{method}' failed.", ex); }
    }

    public ValueTask CreateInline(string elementId, CalcomOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        Validate(options);
        return Invoke("createInline", cancellationToken, elementId, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalcomOptions));
    }
    public ValueTask DestroyInline(string elementId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        return _session is null ? ValueTask.CompletedTask : Invoke("destroyInline", cancellationToken, elementId);
    }
    public ValueTask OpenPopup(CalcomOptions options, CancellationToken cancellationToken = default)
    {
        Validate(options);
        return Invoke("openPopup", cancellationToken, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalcomOptions));
    }
    public ValueTask ClosePopup(CancellationToken cancellationToken = default) =>
        _session is null ? ValueTask.CompletedTask : Invoke("closePopup", cancellationToken);
    public ValueTask CreateFloatingButton(CalcomFloatingButtonOptions options, CancellationToken cancellationToken = default)
    {
        Validate(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ButtonText);
        return Invoke("createFloatingButton", cancellationToken, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalcomFloatingButtonOptions));
    }
    public ValueTask DestroyFloatingButton(CancellationToken cancellationToken = default) =>
        _session is null ? ValueTask.CompletedTask : Invoke("destroyFloatingButton", cancellationToken);

    public ValueTask UpdateInlineUi(string elementId, CalcomUiOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(elementId);
        ArgumentNullException.ThrowIfNull(options);
        return Invoke("updateInlineUi", cancellationToken, elementId, JsonSerializer.Serialize(options, LibraryJsonContext.Default.CalcomUiOptions));
    }

    private static void Validate(CalcomOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CalLink);
        string link = options.CalLink;
        string path = Uri.UnescapeDataString(link.Split('?')[0]);
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.Contains('\\') || path.Contains(':') || path.Contains('#') ||
            link.Contains('#') || link.AsSpan().ContainsAny('\r', '\n', '\t'))
            throw new ArgumentException("CalLink must be a relative booking path such as username/demo, optionally followed by query parameters.", nameof(options));
        foreach (string segment in path.Split('/'))
            if (segment is "." or "..")
                throw new ArgumentException("CalLink cannot contain relative traversal segments.", nameof(options));
        if (options.Config?.AdditionalFields is { } fields)
            foreach (string key in fields.Keys)
                if (string.IsNullOrWhiteSpace(key) || key is "iframeAttrs" or "__proto__" or "constructor" or "prototype" || key.StartsWith("cal.", StringComparison.Ordinal) || key.StartsWith("flag.", StringComparison.Ordinal))
                    throw new ArgumentException("AdditionalFields contains a reserved SDK parameter.", nameof(options));
    }

    private static void ValidateConfiguration(CalcomConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        foreach (string value in new[] { configuration.Origin, configuration.EmbedScriptUrl })
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
                !(uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)) || uri.UserInfo.Length > 0)
                throw new ArgumentException("Cal.com instance and SDK URLs must use HTTPS (HTTP is allowed for loopback development).", nameof(configuration));
        var origin = new Uri(configuration.Origin);
        if (origin.AbsolutePath != "/" || origin.Query.Length > 0 || origin.Fragment.Length > 0)
            throw new ArgumentException("Origin must contain only the scheme, host, and optional port.", nameof(configuration));
    }

    [JSInvokable]
    public async Task ReceiveEvent(string name, string json)
    {
        if (_disposed) return;
        Func<CalcomEvent, Task>? handlers = name switch
        {
            "linkReady" => LinkReady,
            "eventTypeSelected" => EventTypeSelected,
            "linkFailed" => LinkFailed,
            "bookingSuccessfulV2" => BookingSuccessful,
            "bookerReady" => BookerReady,
            _ => null
        };
        if (handlers is null) return;
        CalcomEvent data = JsonSerializer.Deserialize(json, LibraryJsonContext.Default.CalcomEvent)!;
        foreach (Func<CalcomEvent, Task> handler in handlers.GetInvocationList())
            await handler(data);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _lifetime.CancelAsync();
        await _initializer.DisposeAsync();
        try
        {
            if (_session is not null)
            {
                await _session.InvokeVoidAsync("dispose");
                await _session.DisposeAsync();
            }
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        finally
        {
            _reference?.Dispose();
            _lifetime.Dispose();
            LinkReady = null;
            EventTypeSelected = null;
            LinkFailed = null;
            BookingSuccessful = null;
            BookerReady = null;
        }
    }
}
