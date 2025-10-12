namespace EduchemLP.Server.Services;

public sealed class WebSocketHeartbeatService(IWebSocketHub hub, ILogger<WebSocketHeartbeatService> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try {
            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                var beats = hub.GetHeartbeats().ToList();
                foreach (var (channel, heartbeat) in beats) {
                    try { await heartbeat(hub, stoppingToken); }
                    catch (OperationCanceledException) { return; }
                    catch (Exception ex) { logger.LogWarning(ex, "heartbeat for '{channel}' failed", channel); }
                }
            }
        } finally { timer.Dispose(); }
    }
}