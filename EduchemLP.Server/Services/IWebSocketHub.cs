using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Services;

public interface IWebSocketHub {
    // registr klienta do kanalu (napr. "sync")
    void AddClient(string channel, WSClient client);

    // odebrani klienta z kanalu
    void RemoveClient(string channel, uint clientId);

    // snapshot klientu v kanalu
    IReadOnlyCollection<WSClient> GetClients(string channel);

    // broadcast jedne zpravy vsem v kanalu
    Task BroadcastAsync(string channel, string json, CancellationToken ct);

    // odeslani zpravy konkretnimu klientovi v kanalu
    Task SendAsync(string channel, uint clientId, string json, CancellationToken ct);

    // registrace status provideru pro heartbeat (callback se spusti kazdy tick)
    void RegisterHeartbeat(string channel, Func<IWebSocketHub, CancellationToken, Task> heartbeat);

    // vylistovani registrovanych provideru
    IEnumerable<(string channel, Func<IWebSocketHub, CancellationToken, Task> provider)> GetHeartbeats();

    // seznam existujicich kanalu
    IEnumerable<string> Channels { get; }
}