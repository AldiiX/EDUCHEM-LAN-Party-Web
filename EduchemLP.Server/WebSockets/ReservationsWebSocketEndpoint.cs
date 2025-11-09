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

public sealed class ReservationsClient : WSClient {
    public string DisplayName { get; }
    public Account.AccountType? AccountType { get; }
    public string? Class { get; }
    public string? Avatar { get; }
    public string? Banner { get; }
    public bool IsGuest { get; }

    public ReservationsClient(WebSocket socket, Account? account) : base(socket) {
        if (account is null) {
            // host nebo neprihlaseny – drzej se puvodni logiky s nahodnym id a "Guest"
            var rnd = (uint) Random.Shared.Next(10_000, 999_999);
            Id = rnd;
            DisplayName = "Guest";
            AccountType = null;
            IsGuest = true;
        } else {
            Id = (uint) account.Id;
            DisplayName = account.DisplayName;
            AccountType = account.Type;
            Class = account.Class;
            Avatar = account.Avatar;
            Banner = account.Banner;
            IsGuest = false;
        }
    }
}





public sealed class ReservationsWebSocketEndpoint(
    IDatabaseService db,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationsWebSocketEndpoint> logger,
    IDbLoggerService dbLogger,
    IWebSocketHub hub
) : IWebSocketEndpoint {

    public PathString Path => "/ws/reservations";

    // jednoduchy guard, at heartbeat nezaregistrujeme vicekrat
    private static int heartbeatRegistered = 0;

    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct) {
        // auth je nepovinny, muze vratit null (anonymni klient)
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var sessionAccount = await auth.ReAuthAsync(ct);
        var client = new ReservationsClient(socket, sessionAccount);



        // registrace klienta do kanalu "reservations" a poslat vsem okamzity status
        hub.AddClient("reservations", client);
        await BroadcastStatusAsync(ct);
        await SendFullReservationInfoAsync(client, ct);



        // heartbeat - posilani statusu kazdych 15s
        await RegisterHeartbeatAsync();



        // hlavni receive loop
        var buffer = new byte[4 * 1024];
        try {
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
                        }.ToJsonString(), ct
                    );
                    continue;
                }

                switch (action) {
                    case "reserve": {
                        using var reserveScope = scopeFactory.CreateScope();
                        var freshAppSettings = reserveScope.ServiceProvider.GetRequiredService<IAppSettingsService>();

                        // kontrola, zda jsou rezervace povolene (napr. podle casu)
                        if (!await freshAppSettings.AreReservationsEnabledRightNowAsync(ct)) {
                            await client.SendAsync(new
                                {
                                    action = "error",
                                    message = "Rezervace nejsou momentálně povolené."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        // kontrola opravneni rezervovat
                        var accountCanReserve = await IsAccountAbleToReserveAsync(sessionAccount, ct);
                        //Console.WriteLine($"Account {sessionAccount!.Id} can reserve: {accountCanReserve}");

                        if (!accountCanReserve)
                        {
                            await client.SendAsync(new
                                {
                                    action = "error",
                                    message = "Tvůj účet nemá povolené rezervace."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        var room = messageJson?["room"]?.ToString();
                        var computer = messageJson?["computer"]?.ToString();
                        if (string.IsNullOrWhiteSpace(room) && string.IsNullOrWhiteSpace(computer)) break;

                        await using var conn = await db.GetOpenConnectionAsync(ct);
                        if (conn is null) continue;

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
                        _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO,
                            $"Uživatel {sessionAccount.DisplayName} ({sessionAccount.Email}) rezervoval {room ?? computer}.",
                            "reservation", ct
                        );
                    } break;

                    case "deleteReservation": {
                        using var deleteScope = scopeFactory.CreateScope();
                        var freshAppSettings = deleteScope.ServiceProvider.GetRequiredService<IAppSettingsService>();

                        // kontrola, zda jsou rezervace povolene (napr. podle casu)
                        if (!await freshAppSettings.AreReservationsEnabledRightNowAsync(ct)) {
                            await client.SendAsync(new
                                {
                                    action = "error",
                                    message = "Rezervace nejsou momentálně povolené."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        await using var conn = await db.GetOpenConnectionAsync(ct);
                        if (conn is null) continue;

                        await using (var cmd = conn.CreateCommand()) {
                            cmd.CommandText = "DELETE FROM reservations WHERE user_id = @user_id;";
                            cmd.Parameters.AddWithValue("@user_id", sessionAccount!.Id);
                            await cmd.ExecuteNonQueryAsync(ct);
                        }

                        await BroadcastFullReservationInfoAsync(ct);

                        _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO,
                            $"Uživatel {sessionAccount!.DisplayName} ({sessionAccount!.Email}) zrušil rezervaci.",
                            "reservation", ct
                        );
                    } break;

                    case "disconnect": {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", ct);
                    } break;
                }
            }
        }

        // pri konci receive loopu
        finally {
            // odhlaseni klienta
            hub.RemoveClient("reservations", client.Id);

            // poslat okamzity status po odhlaseni
            await BroadcastStatusAsync(CancellationToken.None);
        }
    }



    // ===== logistika =====
    private async Task BroadcastStatusAsync(CancellationToken ct) {
        // pro kazdeho prijemce je obsah trochu jiny (schovani informaci pro anonymni)
        var list = hub.GetClients("reservations").ToList();

        foreach (var r in list) {
            if (r.State != WebSocketState.Open) continue;

            var receiver = (ReservationsClient)r;
            var payload = await BuildStatusPayloadAsync(receiver, ct);
            await hub.SendAsync("reservations", receiver.Id, payload, ct);
        }
    }

    private async Task BroadcastFullReservationInfoAsync(CancellationToken ct) {
        var clients = hub.GetClients("reservations").ToList().Cast<ReservationsClient>();
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

        await hub.SendAsync("reservations", client.Id, payload, ct);
    }

    private async Task RegisterHeartbeatAsync() {
        if (Interlocked.CompareExchange(ref heartbeatRegistered, 1, 0) == 0) {
            hub.RegisterHeartbeat("reservations", async (hub2, token) => {
                // posli aktualni status kazdemu prijemci zvlast (muze byt personalizovany)
                var list = hub2.GetClients("reservations").ToList();
                
                foreach (var r in list) {
                    if (r.State != WebSocketState.Open) continue;

                    var receiver = (ReservationsClient)r;
                    var payload = await BuildStatusPayloadAsync(receiver, token);
                    await hub2.SendAsync("reservations", receiver.Id, payload, token);
                }
            });
        }
    }

    private async Task<bool> IsAccountAbleToReserveAsync(Account? account, CancellationToken ct) {
        if (account == null) return false;

        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return false;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM users WHERE id = @user_id AND enable_reservation = 1";
        cmd.Parameters.AddWithValue("@user_id", account.Id);

        var result = await cmd.ExecuteScalarAsync(ct);
        //Program.Logger.LogInformation("Reservation permission check for account {AccountId}: {Result}", account.Id, result);
        var count = int.TryParse(result?.ToString(), out var c) ? c : 0;
        return count > 0;
    }

    private async Task<string> BuildStatusPayloadAsync(ReservationsClient receiver, CancellationToken ct) {
        var list = hub.GetClients("reservations").ToList();
        var connectedUsers = new JsonArray();
        foreach (var client in list.DistinctBy(x => x.Id)) {
            var c = (ReservationsClient)client;

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

        return json;
    }
}