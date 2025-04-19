using System.Net.WebSockets;

namespace EduchemLP.Server.Classes.Objects;

public class WSClient(WebSocket webSocket) {
    public string UUID { get; private set; } = Guid.NewGuid().ToString();
    public WebSocket WebSocket { get; private set; } = webSocket;



    public async Task DisconnectAsync(string reason = "Closed by server") => await CloseWebSocketAsync(reason);

    public void Disconnect(string reason = "Closed by server") => CloseWebSocketAsync(reason).Wait();

    public async Task CloseWebSocketAsync(string reason = "Closed by server") {
        try {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
            WebSocket.Dispose();
        } catch (Exception _) {/**/}
    }

    public void CloseWebSocket(string reason = "Closed by server") => CloseWebSocketAsync(reason).Wait();

    public async Task BroadcastAsync(string message) {
        if (WebSocket.State == WebSocketState.Open) {
            try {
                await WebSocket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
            } catch (Exception _) { /**/ }
        }
    }

    public void Broadcast(in string message) => BroadcastAsync(message).Wait();
}