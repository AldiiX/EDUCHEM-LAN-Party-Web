using System.Net.WebSockets;

namespace EduchemLP.Server.Classes.Objects;

public class WSClient(WebSocket webSocket) {
    public string UUID { get; private set; } = Guid.NewGuid().ToString();
    public WebSocket WebSocket { get; private set; } = webSocket;
}