using System;
using System.Threading.Tasks;
using Soenneker.Calcom.Blazor.Abstract;
using Soenneker.Calcom.Blazor.Dtos;
using Soenneker.Tests.HostedUnit;
namespace Soenneker.Calcom.Blazor.Tests;
[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class CalcomTests : HostedUnitTest
{
    private readonly ICalcom _calcom;
    public CalcomTests(Host host) : base(host) => _calcom = Resolve<ICalcom>(true);

    [Test]
    public async Task RejectsInvalidPathsAndReservedConfigurationBeforeInterop()
    {
        foreach (string path in new[] { "", "https://cal.com/demo", "//evil.test", "../demo", "user/%2e%2e/demo", "user\\demo", "user/demo#fragment" })
            await Reject(() => _calcom.OpenPopup(new() { CalLink = path }));
        await Reject(() => _calcom.CreateInline("", new() { CalLink = "user/demo" }));
        await Reject(() => _calcom.CreateFloatingButton(new() { CalLink = "user/demo", ButtonText = " " }));
        await Reject(() => _calcom.OpenPopup(new() { CalLink = "user/demo", Config = new() { AdditionalFields = new() { ["iframeAttrs"] = "bad" } } }));
    }

    [Test]
    public async Task DispatchesBookingPayloadAndAwaitsEverySubscriber()
    {
        int calls = 0;
        Func<CalcomEvent, Task> first = async data =>
        {
            await Task.Yield();
            if (data.BookingUid != "booking-123" || data.ElementId != "inline" || data.Namespace != "widget")
                throw new InvalidOperationException("Booking payload was lost.");
            calls++;
        };
        Func<CalcomEvent, Task> second = _ => { calls++; return Task.CompletedTask; };
        _calcom.BookingSuccessful += first;
        _calcom.BookingSuccessful += second;
        try
        {
            await ((Calcom)_calcom).ReceiveEvent("bookingSuccessfulV2", """{"elementId":"inline","namespace":"widget","data":{"uid":"booking-123"}}""");
            await ((Calcom)_calcom).ReceiveEvent("unknown", "{}");
            if (calls != 2) throw new InvalidOperationException("Callbacks were not awaited exactly once.");
        }
        finally { _calcom.BookingSuccessful -= first; _calcom.BookingSuccessful -= second; }
    }
    private static async Task Reject(Func<ValueTask> operation)
    {
        try { await operation(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Invalid configuration was accepted.");
    }
}
