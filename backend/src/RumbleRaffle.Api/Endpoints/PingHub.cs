using Microsoft.AspNetCore.SignalR;

namespace RumbleRaffle.Api.Endpoints;

// Minimal, deliberately generic hub proving the SignalR round trip works at
// all (1.8) -- a client connects, something broadcasts to it, it receives
// the message -- before any domain-specific real-time features (event
// state changes, elimination updates, etc.) exist to hang off it. Likely
// gets renamed or joined by other hubs once those needs are known; for now
// it has no methods of its own; PingHubTests broadcasts to it directly via
// IHubContext<PingHub>, the same way real application code eventually will.
public class PingHub : Hub
{
}
