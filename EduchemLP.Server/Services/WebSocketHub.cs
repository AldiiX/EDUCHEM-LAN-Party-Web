using System.Collections.Concurrent;
using System.Net.WebSockets;
using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Services;

public sealed class WebSocketHub : IWebSocketHub {
    // store klientu: channel -> (clientId -> client)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<uint, WSClient>> channels = new();

    // registry provideru: channel -> callback
    private readonly ConcurrentDictionary<string, Func<IWebSocketHub, CancellationToken, Task>> providers = new();

    public IEnumerable<string> Channels => channels.Keys;

    public void AddClient(string channel, WSClient client) {
        var dict = channels.GetOrAdd(channel, _ => new ConcurrentDictionary<uint, WSClient>());
        dict[client.Id] = client;
    }

    public void RemoveClient(string channel, uint clientId) {
        if (!channels.TryGetValue(channel, out var dict)) return;

        var client = dict.GetValueOrDefault(clientId);
        client?.Abort();
        dict.TryRemove(clientId, out _);
    }

    public IReadOnlyCollection<WSClient> GetClients(string channel) {
        if (!channels.TryGetValue(channel, out var dict)) return [];

        foreach (var (key, ws) in dict) {
            if (ws.State != WebSocketState.Open) {
                dict.TryRemove(key, out _);
            }
        }

        return dict.Values.ToList();
    }

    public async Task BroadcastAsync(string channel, string json, CancellationToken ct) {
        if (!channels.TryGetValue(channel, out var dict)) return;
        var list = dict.Values.ToList();
        var clientsToRemove = new List<WSClient>();

        foreach (var c in list) {
            if (c.State != WebSocketState.Open) {
                clientsToRemove.Add(c);
                continue;
            }
            try {
                await c.SendAsync(json, ct);
            } catch {
                clientsToRemove.Add(c);
            }
        }

        foreach (var c in clientsToRemove) {
            RemoveClient(channel, c.Id);
        }
    }

    public async Task SendAsync(string channel, uint clientId, string payload, CancellationToken ct) {
        if (!channels.TryGetValue(channel, out var dict)) return;
        if (!dict.TryGetValue(clientId, out var c)) return;
        if (c.State != WebSocketState.Open) {
            RemoveClient(channel, clientId);
            return;
        }

        try {
            await c.SendAsync(payload, ct);
        } catch {
            RemoveClient(channel, clientId);
        }
    }

    public void RegisterHeartbeat(string channel, Func<IWebSocketHub, CancellationToken, Task> provider) {
        // pokud uz provider pro dany kanal existuje, nahrad ho poslednim (idempotentni)
        providers[channel] = provider;
        // zajisti, ze kanal existuje
        channels.GetOrAdd(channel, _ => new ConcurrentDictionary<uint, WSClient>());
    }

    public IEnumerable<(string channel, Func<IWebSocketHub, CancellationToken, Task> provider)> GetHeartbeats() {
        foreach (var kv in providers) {
            yield return (kv.Key, kv.Value);
        }
    }
}