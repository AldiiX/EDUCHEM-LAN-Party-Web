using System.Net.WebSockets;

namespace EduchemLP.Server.WebSockets;

public interface IWebSocketEndpoint {
    // cesta, na kterou endpoint reaguje, napr. "/ws/chat"
    PathString Path { get; }

    // vlastni obsluha websocketu
    Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct);
}