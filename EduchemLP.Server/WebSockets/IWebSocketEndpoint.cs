using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EduchemLP.Server.WebSockets;

public interface IWebSocketEndpoint {
    // cesta, na kterou endpoint reaguje, napr. "/ws/chat"
    PathString Path { get; }

    // vlastni obsluha websocketu
    Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct);
}