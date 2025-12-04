using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Services;

namespace EduchemLP.Server.WebSockets;





public class SyncWebSocketEndpoint(IServiceScopeFactory scopeFactory, IWebSocketHub hub) : IWebSocketEndpoint {
    public PathString Path => "/ws/sync";

    // jednoduchy guard, at heartbeat nezaregistrujeme vicekrat
    private static int heartbeatRegistered = 0;

    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct) {
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var appSettings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

        var sessionAccount = await auth.ReAuthAsync(ct);
        var client = new ReservationsClient(socket, sessionAccount);

        // registrace klienta do kanalu "sync" a poslat vsem okamzity status
        hub.AddClient("sync", client);
        var welcome = new { action = "clientJoined", numberOfConnectedUsers = hub.GetClients("sync").Count }.ToJsonString();
        await hub.BroadcastAsync("sync", welcome, ct);



        // heartbeat - posilani statusu kazdych 15s
        await RegisterHeartbeatAsync();



        // hlavni receive loop
        var buffer = new byte[4 * 1024];
        while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open) {
            WebSocketReceiveResult result;

            try {
                result = await socket.ReceiveAsync(buffer, ct);
            } catch (OperationCanceledException) {
                break;
            } catch (WebSocketException) {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close) break;

            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (string.IsNullOrWhiteSpace(messageString)) continue;

            JsonNode? messageJson;
            try {
                messageJson = JsonNode.Parse(messageString);
            } catch (JsonException) {
                continue;
            }

            var action = messageJson?["action"]?.ToString();
            if (action is null) continue;

            switch (action) { }
        }

        // odhlaseni klienta
        hub.RemoveClient("sync", client.Id);

        // poslat okamzity status po odhlaseni
        var bye = new { action = "clientLeft", numberOfConnectedUsers = hub.GetClients("sync").Count }.ToJsonString();
        await hub.BroadcastAsync("sync", bye, CancellationToken.None);
    }


    private async Task BroadcastStatusAsync(CancellationToken ct) {
        // pro kazdeho prijemce je obsah trochu jiny (schovani class pro nektere)
        var list = hub.GetClients("sync").ToList();

        foreach (var r in list) {
            if (r.State != WebSocketState.Open) continue;

            var connectedUsers = new JsonArray();
            foreach (var c in list.DistinctBy(x => x.Id)) {
                var client = (ReservationsClient)c;
                var receiver = (ReservationsClient)r;

                // pokud c nebo prijemce je anonym, posle se "unknown"
                if (client.AccountType is null || receiver.AccountType is null) {
                    connectedUsers.Add("unknown");
                    continue;
                }

                connectedUsers.Add(new JsonObject {
                    ["id"] = client.Id,
                    ["displayName"] = client.DisplayName,
                    ["class"] = client.AccountType > Account.AccountType.STUDENT ? client.Class : null
                });
            }

            var json = new {
                action = "status",
                numberOfConnectedUsers = list.Count,
            }.ToJsonString();

            await SafeSendAsync((ReservationsClient) r, json, ct);
        }
    }

    private static async Task SafeSendAsync(ReservationsClient client, string json, CancellationToken ct) {
        if (client.State != WebSocketState.Open) return;
        try {
            await client.SendAsync(json, ct);
        } catch {
            //
        }
    }

    private async Task RegisterHeartbeatAsync() {
        if (Interlocked.CompareExchange(ref heartbeatRegistered, 1, 0) == 0) {
            hub.RegisterHeartbeat("sync", async (hub2, token) => {
                // posli aktualni status kazdemu prijemci zvlast (muze byt personalizovany)
                var clients = hub2.GetClients("sync").ToList();
                foreach (var receiver in clients) {
                    if (receiver.State != WebSocketState.Open) continue;

                    var connectedUsers = new JsonArray();
                    foreach (var c in clients.DistinctBy(x => x.Id)) {
                        connectedUsers.Add(c.Id);
                    }

                    var payload = new {
                        action = "status",
                        numberOfConnectedUsers = clients.Count,
                        //connectedUsers
                    }.ToJsonString();

                    await hub2.SendAsync("sync", receiver.Id, payload, token);
                }
            });
        }
    }
}