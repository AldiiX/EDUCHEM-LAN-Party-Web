using EduchemLP.Server.WebSockets;

namespace EduchemLP.Server.Middlewares;

public sealed class WebSocketMiddleware {
    private readonly RequestDelegate _next;
    private readonly IReadOnlyDictionary<PathString, IWebSocketEndpoint> _endpoints;

    public WebSocketMiddleware(RequestDelegate next, IEnumerable<IWebSocketEndpoint> endpoints) {
        _next = next;
        _endpoints = endpoints.ToDictionary(e => e.Path);
    }

    public async Task InvokeAsync(HttpContext context) {
        var path = context.Request.Path;

        if (context.WebSockets.IsWebSocketRequest && _endpoints.TryGetValue(path, out var endpoint)) {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            await endpoint.HandleAsync(context, socket, linkedCts.Token);
            return;
        }

        await _next(context);
    }
}