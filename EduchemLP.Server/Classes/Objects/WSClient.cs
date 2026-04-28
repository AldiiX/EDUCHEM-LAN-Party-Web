using System.Net.WebSockets;
using System.Text;

namespace EduchemLP.Server.Classes.Objects;

public abstract class WSClient(WebSocket socket, uint? id = null) {
    private readonly SemaphoreSlim sendLock = new(1, 1);

    public uint Id { get; set; } = id ?? (uint) new Random().NextInt64(1, uint.MaxValue);
    public WebSocketState State => socket.State;

    public async Task SendAsync(string json, CancellationToken ct) {
        var bytes = Encoding.UTF8.GetBytes(json);

        await sendLock.WaitAsync(ct);
        try {
            if (socket.State != WebSocketState.Open) return;

            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
        }
        finally {
            sendLock.Release();
        }
    }


    public async Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken ct) {
        await sendLock.WaitAsync(ct);
        try {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived) {
                await socket.CloseAsync(status, reason, ct);
            }
        }
        finally {
            sendLock.Release();
        }
    }

    public void Abort() {
        try { socket.Abort(); } catch { /* ignore */ }
    }
}
