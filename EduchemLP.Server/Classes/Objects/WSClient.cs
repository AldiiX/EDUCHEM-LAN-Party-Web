using System.Net.WebSockets;
using System.Text;

namespace EduchemLP.Server.Classes.Objects;

public abstract class WSClient {
    private readonly WebSocket _socket;

    public uint Id { get; set; }
    public WebSocketState State => _socket.State;

    public WSClient(WebSocket socket, uint? id = null) {
        _socket = socket;
        Id = id ?? (uint) new Random().NextInt64(1, uint.MaxValue);
    }

    public async Task SendAsync(string json, CancellationToken ct) {
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
    }


    public Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken ct)
        => _socket.CloseAsync(status, reason, ct);

    public void Abort() {
        try { _socket.Abort(); } catch { /* ignore */ }
    }
}