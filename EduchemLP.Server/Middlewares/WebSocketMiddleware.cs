using System.Net.WebSockets;
using EduchemLP.Server.Services;

namespace EduchemLP.Server.Middlewares;

public class WebSocketMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {
        // reservations websocket
        if (context.Request.Path == "/ws/reservations") {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await WSReservations.HandleQueueAsync(webSocket);
            } else {
                context.Response.StatusCode = 400;
            }
        }
        else if (context.Request.Path == "/ws/chat") {
            if (context.WebSockets.IsWebSocketRequest) {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await WSChat.HandleQueueAsync(webSocket);
            } else {
                context.Response.StatusCode = 400;
            }
        }

        else await next(context);
    }
}