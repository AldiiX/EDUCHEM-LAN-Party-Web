using System.Collections.Concurrent;

namespace EduchemLPR.Services;





public class SSEService {
    private readonly ConcurrentDictionary<Guid, StreamWriter> _clients = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public SSEService() {
        // startnutí tasku na pingování clientů (kvůli cloudflaru, který to furt odpojuje)
        Task.Run(() => StartPingingClientsAsync(_cancellationTokenSource.Token));
    }

    public Guid RegisterClient(StreamWriter writer) {
        var clientId = Guid.NewGuid();
        _clients.TryAdd(clientId, writer);
        return clientId;
    }

    public void UnregisterClient(Guid clientId) => _clients.TryRemove(clientId, out _);

    public async Task NotifyClientsAsync(object data) {
        var message = $"data: {System.Text.Json.JsonSerializer.Serialize(data)}\n\n";
        await BroadcastToClientsAsync(message);
    }

    public int GetConnectedClientsCount() => _clients.Count;

    private async Task StartPingingClientsAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            await Task.Delay(60000, cancellationToken); // každých 60 sekund
            await BroadcastToClientsAsync("data: {\"type\":\"ping\", \"message\":\"I'm still alive!\", \"datetime\": \"" + DateTime.Now + "\"}\n\n");
        }
    }

    private async Task BroadcastToClientsAsync(string message) {
        var disconnectedClients = new List<Guid>();

        foreach (var client in _clients) {
            try {
                await client.Value.WriteAsync(message);
                await client.Value.FlushAsync();
            } catch {
                disconnectedClients.Add(client.Key);
            }
        }

        // odstranění odpojených clientů
        foreach (var clientId in disconnectedClients) {
            _clients.TryRemove(clientId, out _);
        }
    }

    public void Dispose() {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}