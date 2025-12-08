using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EduchemLP.Server.Classes.Objects;

public abstract class WSClient(WebSocket socket, uint? id = null) {

    public uint Id { get; set; } = id ?? (uint) new Random().NextInt64(1, uint.MaxValue);
    public WebSocketState State => socket.State;

    public async Task SendAsync(string json, CancellationToken ct) {
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
    }


    public Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken ct)
        => socket.CloseAsync(status, reason, ct);

    public void Abort() {
        try { socket.Abort(); } catch { /* ignore */ }
    }
}