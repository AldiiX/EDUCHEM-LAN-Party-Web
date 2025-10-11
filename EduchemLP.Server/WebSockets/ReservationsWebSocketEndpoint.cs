using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;

namespace EduchemLP.Server.WebSockets;

public sealed class ReservationsClient {
    private readonly WebSocket _socket;

    public int Id { get; }
    public string DisplayName { get; }
    public Account.AccountType? AccountType { get; }
    public string? Class { get; }
    public string? Avatar { get; }
    public string? Banner { get; }
    public bool IsGuest { get; }

    public ReservationsClient(WebSocket socket, Account? account) {
        _socket = socket;
        if (account is null) {
            // host nebo neprihlaseny – drzej se puvodni logiky s nahodnym id a "Guest"
            var rnd = Random.Shared.Next(10_000, 999_999);
            Id = rnd;
            DisplayName = "Guest";
            AccountType = null;
            IsGuest = true;
        } else {
            Id = account.Id;
            DisplayName = account.DisplayName;
            AccountType = account.Type;
            Class = account.Class;
            Avatar = account.Avatar;
            Banner = account.Banner;
            IsGuest = false;
        }
    }

    public async Task SendAsync(string json, CancellationToken ct) {
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: ct);
    }

    public WebSocketState State => _socket.State;

    public Task CloseAsync(WebSocketCloseStatus status, string reason, CancellationToken ct)
        => _socket.CloseAsync(status, reason, ct);

    public void Abort() {
        try { _socket.Abort(); } catch { /* ignore */ }
    }
}





public sealed class ReservationsWebSocketEndpoint(
    IDatabaseService db,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationsWebSocketEndpoint> logger,
    IDbLoggerService dbLogger
) : IWebSocketEndpoint {

    public PathString Path => "/ws/reservations";

    // sprava klientu (dle endpointu)
    private readonly ConcurrentDictionary<int, ReservationsClient> clients = new();

    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct) {
        // auth je nepovinny, muze vratit null (anonymni klient)
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var appSettings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

        var sessionAccount = await auth.ReAuthAsync(ct);
        var client = new ReservationsClient(socket, sessionAccount);

        clients[client.Id] = client;

        // po pripojeni: poslani kompletniho prehledu a statusu
        await SendFullReservationInfoAsync(client, ct);
        await BroadcastStatusAsync(ct);

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

            // anonym muze posilat jen "disconnect"; ostatni akce vyzaduji login
            if (sessionAccount is null && action != "disconnect") {
                await client.SendAsync(new {
                    action = "error",
                    message = "Nejsi přihlášen."
                }.ToJsonString(), ct);
                continue;
            }

            switch (action) {
                case "reserve": {
                    if (!await appSettings.AreReservationsEnabledRightNowAsync(ct)) break;

                    // kontrola opravneni rezervovat
                    if (sessionAccount is { EnableReservation: false }) {
                        await client.SendAsync(new {
                            action = "error",
                            message = "Tvůj účet nemá povolené rezervace."
                        }.ToJsonString(), ct);
                        break;
                    }

                    var room = messageJson?["room"]?.ToString();
                    var computer = messageJson?["computer"]?.ToString();
                    if (string.IsNullOrWhiteSpace(room) && string.IsNullOrWhiteSpace(computer)) break;

                    await using var conn = await db.GetOpenConnectionAsync(ct);
                    if (conn is null) break;

                    await using (var cmd = conn.CreateCommand()) {
                        cmd.CommandText =
                        """
                            DELETE FROM reservations WHERE user_id = @user_id;
                            INSERT INTO reservations (user_id, room_id, computer_id) VALUES (@user_id, @room_id, @computer_id);
                        """;
                        cmd.Parameters.AddWithValue("@user_id", sessionAccount!.Id);
                        cmd.Parameters.AddWithValue("@room_id", (object?)room ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@computer_id", (object?)computer ?? DBNull.Value);
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    // posli novy stav vsem
                    await BroadcastFullReservationInfoAsync(ct);

                    // log
                    _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO, $"Uživatel {sessionAccount.DisplayName} ({sessionAccount.Email}) rezervoval {room ?? computer}.", "reservation", ct);
                } break;

                case "deleteReservation": {
                    if (!await appSettings.AreReservationsEnabledRightNowAsync(ct)) break;

                    await using var conn = await db.GetOpenConnectionAsync(ct);
                    if (conn is null) break;

                    await using (var cmd = conn.CreateCommand()) {
                        cmd.CommandText = "DELETE FROM reservations WHERE user_id = @user_id;";
                        cmd.Parameters.AddWithValue("@user_id", sessionAccount!.Id);
                        await cmd.ExecuteNonQueryAsync(ct);
                    }

                    await BroadcastFullReservationInfoAsync(ct);

                    _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO, $"Uživatel {sessionAccount!.DisplayName} ({sessionAccount!.Email}) zrušil rezervaci.", "reservation", ct);
                } break;

                case "disconnect": {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", ct);
                } break;
            }
        }

        // odhlaseni klienta + status
        clients.TryRemove(client.Id, out _);
        await BroadcastStatusAsync(ct);
    }

    // ===== logistika =====

    private async Task BroadcastStatusAsync(CancellationToken ct) {
        // pro kazdeho prijemce je obsah trochu jiny (schovani class pro nektere)
        var list = clients.Values.ToList();
        foreach (var receiver in list) {
            if (receiver.State != WebSocketState.Open) continue;

            var connectedUsers = new JsonArray();
            foreach (var c in list.DistinctBy(x => x.Id)) {
                // pokud c nebo prijemce je anonym, posle se "unknown"
                if (c.AccountType is null || receiver.AccountType is null) {
                    connectedUsers.Add("unknown");
                    continue;
                }

                connectedUsers.Add(new JsonObject {
                    ["id"] = c.Id,
                    ["displayName"] = c.DisplayName,
                    ["class"] = c.AccountType > Account.AccountType.STUDENT ? c.Class : null
                });
            }

            var json = new {
                action = "status",
                connectedUsers
            }.ToJsonString();

            await SafeSendAsync(receiver, json, ct);
        }
    }

    private async Task BroadcastFullReservationInfoAsync(CancellationToken ct) {
        var clients = this.clients.Values.ToList();
        foreach (var c in clients) {
            if (c.State != WebSocketState.Open) continue;
            await SendFullReservationInfoAsync(c, ct);
        }
    }

    private async Task SendFullReservationInfoAsync(ReservationsClient client, CancellationToken ct) {
        using var scope = scopeFactory.CreateScope();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var computers = scope.ServiceProvider.GetRequiredService<IComputerRepository>();

        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return;

        // nacte se seznamy pokoju a pc
        var roomsTask = rooms.GetAllAsync(ct);
        var computersTask = computers.GetAllAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
        """
            SELECT 
                res.*, 
                usr.id AS user_id, 
                usr.display_name AS user_display_name, 
                usr.class AS user_class,
                usr.avatar AS user_avatar,
                usr.banner AS user_banner,
                COALESCE(room.id, NULL) AS room_id, 
                COALESCE(room.limit_of_seats, NULL) AS room_limit, 
                COALESCE(room.available, NULL) AS room_available,
                COALESCE(room.label, NULL) AS room_label,
                COALESCE(room.image, NULL) AS room_image,
                COALESCE(comp.id, NULL) AS computer_id,
                COALESCE(comp.is_teachers_pc, NULL) AS computer_is_teachers_pc,
                COALESCE(comp.available, NULL) AS computer_available,
                COALESCE(comproom.image, NULL) AS computer_image
            FROM reservations res
            LEFT JOIN users usr ON res.user_id = usr.id
            LEFT JOIN rooms room ON res.room_id = room.id
            LEFT JOIN computers comp ON res.computer_id = comp.id 
            LEFT JOIN rooms comproom ON comp.room_id = comproom.id
            WHERE comp.available = 1 OR room.available = 1;
        """;

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var reservations = new JsonArray();
        while (await reader.ReadAsync(ct)) {
            JsonNode? userObj = client.IsGuest ? "unknown" : reader.GetValueOrNull<int>("user_id") is null ? null : new JsonObject {
                ["id"] = reader.GetValueOrNull<int>("user_id"),
                ["displayName"] = reader.GetStringOrNull("user_display_name"),
                ["class"] = client.AccountType.HasValue && client.AccountType.Value > Account.AccountType.STUDENT
                            ? reader.GetStringOrNull("user_class")
                            : null,
                ["avatar"] = reader.GetStringOrNull("user_avatar"),
                ["banner"] = reader.GetStringOrNull("user_banner")
            };

            var roomObj = reader.GetStringOrNull("room_id") is null ? null : new JsonObject {
                ["id"] = reader.GetString("room_id"),
                ["label"] = reader.GetStringOrNull("room_label") ?? reader.GetString("room_id"),
                ["limitOfSeats"] = reader.GetValueOrNull<int>("room_limit"),
                ["available"] = reader.GetValueOrNull<bool>("room_available"),
                ["image"] = reader.GetStringOrNull("room_image")
            };

            var compObj = reader.GetStringOrNull("computer_id") is null ? null : new JsonObject {
                ["id"] = reader.GetStringOrNull("computer_id"),
                ["isTeachersPC"] = reader.GetValueOrNull<bool>("computer_is_teachers_pc"),
                ["available"] = reader.GetValueOrNull<bool>("computer_available"),
                ["image"] = reader.GetStringOrNull("computer_image")
            };

            var item = new JsonObject {
                ["user"] = userObj == null || userObj.ToString() == "unknown" ? "unknown" : userObj,
                ["room"] = roomObj,
                ["computer"] = compObj,
                ["note"] = reader.GetStringOrNull("note"),
                ["createdAt"] = reader.GetDateTime("created_at")
            };

            reservations.Add(item);
        }

        var roomsLocal = await roomsTask;
        var computersLocal = await computersTask;

        var payload = new {
            action = "fetchAll",
            reservations,
            computers = computersLocal,
            rooms = roomsLocal
        }.ToJsonString();

        await SafeSendAsync(client, payload, ct);
    }

    private static async Task SafeSendAsync(ReservationsClient client, string json, CancellationToken ct) {
        if (client.State != WebSocketState.Open) return;
        try {
            await client.SendAsync(json, ct);
        } catch {
            //
        }
    }
}