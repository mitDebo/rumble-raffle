using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RumbleRaffle.Api.Hubs;
using RumbleRaffle.Api.IntegrationTests.Scaffolding;
using Xunit;

namespace RumbleRaffle.Api.IntegrationTests.Hubs;

// Proves the SignalR round trip actually works end to end: a real client
// connects to the real hub over the test server's in-process transport,
// and a message broadcast from the server side (via IHubContext<PingHub>,
// exactly how real application code would do it -- not the test reaching
// into the hub's internals) is actually received by that client. No
// database needed, so this reuses NoDatabaseApiFactory rather than a new
// fixture.
public class PingHubTests : IClassFixture<NoDatabaseApiFactory>
{
    private readonly NoDatabaseApiFactory _factory;

    public PingHubTests(NoDatabaseApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConnectedClient_ReceivesBroadcastPing()
    {
        // HttpMessageHandlerFactory points the SignalR client at the test
        // server's in-process pipeline instead of a real socket -- the
        // same trick WebApplicationFactory's own CreateClient() uses under
        // the hood, just wired up manually since HubConnectionBuilder
        // doesn't have a CreateClient()-style helper of its own.
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/ping"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var pingReceived = new TaskCompletionSource<string>();
        // Registered before StartAsync so there's no window where the
        // server could broadcast before this client is listening.
        connection.On<string>("Ping", message => pingReceived.TrySetResult(message));

        // Fails right now with a 404 during negotiation -- nothing maps
        // "hubs/ping" yet. That's the point: this is 1.8's red test.
        await connection.StartAsync();

        var hubContext = _factory.Services.GetRequiredService<IHubContext<PingHub>>();
        await hubContext.Clients.All.SendAsync("Ping", "pong");

        var completed = await Task.WhenAny(pingReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(pingReceived.Task, completed);
        Assert.Equal("pong", await pingReceived.Task);
    }
}
