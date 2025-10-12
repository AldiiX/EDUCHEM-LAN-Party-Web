using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Services;

namespace EduchemLP.Server.WebSockets;



public sealed class ChatClient : WSClient {

    public ChatClient(WebSocket socket, Account account) : base(socket) {
        Id = (uint) account.Id;
        DisplayName = account.DisplayName;
        AccountType = account.Type;
        Class = account.Class;
        Avatar = account.Avatar;
        Banner = account.Banner;
    }

    public string DisplayName { get; }
    public Account.AccountType AccountType { get; }
    public string? Class { get; }
    public string? Avatar { get; }
    public string? Banner { get; }
}



public sealed class ChatWebSocketEndpoint(
    IDatabaseService db,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatWebSocketEndpoint> logger,
    IWebSocketHub hub
) : IWebSocketEndpoint {

    public PathString Path => "/ws/chat";

    // jednoduchy guard, at heartbeat nezaregistrujeme vicekrat
    private static int heartbeatRegistered = 0;


    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct) {
        // auth
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var sessionUser = await auth.ReAuthAsync(ct);
        if (sessionUser is null) {
            await SafeCloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "Unauthorized", ct);
            return;
        }

        if (!await EnsureChatEnabled(socket, ct)) return;



        // registrace klienta (vypne duplicitni spojeni se stejnym id)
        var client = new ChatClient(socket, sessionUser);
        var clients = hub.GetClients("chat").ToDictionary(c => c.Id, c => c);
        if (clients.TryGetValue(client.Id, out var existing)) {
            await existing.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Nové připojení z jiného zařízení.", ct);
            hub.RemoveClient("chat", client.Id);
        }

        hub.AddClient("chat", client);



        // posli inicialni zpravy + stav online uzivatelu
        await SendInitialChatAsync(client, ct);
        await BroadcastConnectedUsersAsync(ct);



        // spustit heartbeat (vyhazuje neaktivni klienty a posila stav online uzivatelu)
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

                if (!await EnsureChatEnabled(socket, ct)) break;

                JsonNode? messageJson;
                try {
                    messageJson = JsonNode.Parse(messageString);
                } catch (JsonException) {
                    continue;
                }

                var action = messageJson?["action"]?.ToString();
                if (action is null) continue;

                switch (action) {
                    case "sendMessage": {
                        var text = messageJson?["message"]?.ToString();
                        if (string.IsNullOrWhiteSpace(text)) break;

                        var saved = await SaveMessageToDbAsync(client, text, ct);
                        if (saved is null) {
                            await client.SendAsync(new JsonObject {
                                    ["action"] = "error",
                                    ["message"] = "Chyba při ukládání zprávy do databáze."
                                }.ToJsonString(JsonSerializerOptions.Web), ct
                            );
                            break;
                        }

                        await hub.BroadcastAsync("chat", saved.ToJsonString(JsonSerializerOptions.Web), ct);
                    }
                        break;

                    case "deleteMessage": {
                        var uuid = messageJson?["uuid"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(uuid)) {
                            await DeleteMessageAsync(client, uuid!, ct);
                        }
                    }
                        break;

                    case "loadOlderMessages": {
                        var beforeUuid = messageJson?["beforeUuid"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(beforeUuid)) {
                            await SendOlderMessagesAsync(client, beforeUuid!, ct);
                        }
                    }
                        break;
                }
            }
        }

        // po skonceni receivu
        finally {
            // odhlaseni klienta
            hub.RemoveClient("chat", client.Id);

            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); } catch { /* ignore */ }

            // poslat okamzity status po odhlaseni
            await BroadcastConnectedUsersAsync(CancellationToken.None);
        }
    }

    // logistika

    private async Task<bool> EnsureChatEnabled(WebSocket socket, CancellationToken ct) {
        using var scope = scopeFactory.CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

        if (await appSettings.GetChatEnabledAsync(ct)) return true;

        // odpojit vsechny a vycistit
        var clients = hub.GetClients("chat").ToDictionary(c => c.Id, c => c);
        foreach (var c in clients.Values) {
            await c.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Chat je vypnutý.", CancellationToken.None);
            hub.RemoveClient("chat", c.Id);
        }

        _ = SafeCloseAsync(socket, WebSocketCloseStatus.NormalClosure, "Chat je vypnutý.", ct);
        return false;
    }

    private async Task SendInitialChatAsync(ChatClient client, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
        """
            SELECT 
                c.*, 
                u.display_name AS author_name, 
                u.avatar AS author_avatar,
                u.class AS author_class,
                u.account_type AS author_account_type,
                u.banner AS author_banner
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.deleted = 0
            ORDER BY `date` DESC
            LIMIT 20
        """;

        await using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, ct);
        var messages = new JsonArray();

        while (await reader.ReadAsync(ct)) {
            var userId = reader.GetValueOrNull<int>("user_id");
            var authorName = reader.GetStringOrNull("author_name");
            if (userId is null || authorName is null) continue;

            var msg = CreateMessageObject(
                reader.GetString("uuid"),
                reader.GetInt32("user_id"),
                authorName,
                reader.GetStringOrNull("author_avatar"),
                reader.GetString("author_account_type"),
                reader.GetStringOrNull("author_class"),
                reader.GetString("message"),
                reader.GetDateTime("date"),
                reader.GetStringOrNull("author_banner"),
                reader.GetBoolean("deleted"),
                reader.GetStringOrNull("replying_to_uuid"),
                client
            );

            messages.Add(msg);
        }

        await client.SendAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages
        }.ToJsonString(JsonSerializerOptions.Web), ct);
    }

    private async Task<JsonObject?> SaveMessageToDbAsync(ChatClient client, string message, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return null;

        await using var cmd = conn.CreateCommand();

        var uuid = Guid.NewGuid().ToString();
        cmd.CommandText =
        """
            INSERT INTO chat (uuid, user_id, message, date, deleted, replying_to_uuid)
            VALUES (@uuid, @userId, @message, NOW(), 0, @replyingToUuid);

            SELECT 
                c.*, 
                u.display_name AS author_name, 
                u.avatar AS author_avatar,
                u.class AS author_class,
                u.account_type AS author_account_type,
                u.banner AS author_banner
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.uuid = @uuid
        """;

        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@userId", client.Id);
        cmd.Parameters.AddWithValue("@message", message);
        cmd.Parameters.AddWithValue("@replyingToUuid", DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var userId = reader.GetValueOrNull<int>("user_id");
        var authorName = reader.GetStringOrNull("author_name");
        if (userId is null || authorName is null) return null;

        return new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = new JsonArray {
                CreateMessageObject(
                    uuid,
                    userId.Value,
                    authorName,
                    reader.GetStringOrNull("author_avatar"),
                    reader.GetString("author_account_type"),
                    reader.GetStringOrNull("author_class"),
                    message,
                    reader.GetDateTime("date"),
                    reader.GetStringOrNull("author_banner"),
                    reader.GetBoolean("deleted"),
                    reader.GetStringOrNull("replying_to_uuid"),
                    client
                )
            }
        };
    }

    private async Task DeleteMessageAsync(ChatClient client, string messageUuid, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return;

        // kdyz neni teacher/admin, smi smazat jen vlastni zpravu
        if (client.AccountType < Account.AccountType.TEACHER) {
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT user_id FROM chat WHERE uuid = @uuid";
            checkCmd.Parameters.AddWithValue("@uuid", messageUuid);

            var ownerObj = await checkCmd.ExecuteScalarAsync(ct);
            if (ownerObj is not int ownerId || ownerId != client.Id) {
                await client.SendAsync(new JsonObject {
                    ["action"] = "error",
                    ["message"] = "Nemáte oprávnění smazat tuto zprávu."
                }.ToJsonString(JsonSerializerOptions.Web), ct);
                return;
            }
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE chat SET deleted = 1 WHERE uuid = @uuid";
        cmd.Parameters.AddWithValue("@uuid", messageUuid);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        if (affected > 0) {
            var payload = new JsonObject {
                ["action"] = "deleteMessage",
                ["uuid"] = messageUuid
            }.ToJsonString(JsonSerializerOptions.Web);

            await hub.BroadcastAsync("chat", payload, ct);
        }
    }

    private async Task SendOlderMessagesAsync(ChatClient client, string beforeUuid, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn is null) return;

        // ziska datum referencni zpravy
        await using var dateCmd = conn.CreateCommand();
        dateCmd.CommandText = "SELECT `date` FROM chat WHERE uuid = @uuid";
        dateCmd.Parameters.AddWithValue("@uuid", beforeUuid);

        var beforeDateObj = await dateCmd.ExecuteScalarAsync(ct);
        if (beforeDateObj is not DateTime beforeDate) {
            return;
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
        """
            SELECT 
                c.*, 
                u.display_name AS author_name, 
                u.avatar AS author_avatar,
                u.class AS author_class,
                u.account_type AS author_account_type,
                u.banner AS author_banner
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.date < @beforeDate AND c.deleted = 0
            ORDER BY c.date DESC
            LIMIT 20
        """;
        cmd.Parameters.AddWithValue("@beforeDate", beforeDate);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var messages = new JsonArray();

        while (await reader.ReadAsync(ct)) {
            var userId = reader.GetValueOrNull<int>("user_id");
            var authorName = reader.GetStringOrNull("author_name");
            if (userId is null || authorName is null) continue;

            messages.Add(CreateMessageObject(
                reader.GetString("uuid"),
                userId.Value,
                authorName,
                reader.GetStringOrNull("author_avatar"),
                reader.GetString("author_account_type"),
                reader.GetStringOrNull("author_class"),
                reader.GetString("message"),
                reader.GetDateTime("date"),
                reader.GetStringOrNull("author_banner"),
                reader.GetBoolean("deleted"),
                reader.GetStringOrNull("replying_to_uuid"),
                client
            ));
        }

        if (messages.Count == 0) {
            await client.SendAsync(new JsonObject { ["action"] = "noMoreMessagesToFetch" }.ToJsonString(JsonSerializerOptions.Web), ct);
            return;
        }

        await client.SendAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages,
            ["isLoadMoreAction"] = true
        }.ToJsonString(JsonSerializerOptions.Web), ct);
    }

    private async Task BroadcastConnectedUsersAsync(CancellationToken ct) {
        var clients = hub.GetClients("chat");

        var users = clients.Select(c => new {
            id = c.Id,
            name = (c as ChatClient)?.DisplayName,
            avatar = (c as ChatClient)?.Avatar
        }).ToList();

        var json = JsonSerializer.Serialize(new {
            action = "updateConnectedUsers",
            users
        }, JsonSerializerOptions.Web);

        //Program.Logger.LogInformation("Broadcasting connected users: {x}", users);
        await hub.BroadcastAsync("chat", json, ct);
    }

    private static JsonObject CreateMessageObject(
        string uuid,
        int userId,
        string userName,
        string? userAvatar,
        string userAccountType,
        string? userClass,
        string message,
        DateTime date,
        string? userBanner,
        bool deleted,
        string? replyingToUuid,
        ChatClient? client = null
    ) {
        var obj = new JsonObject {
            ["uuid"] = uuid,
            ["author"] = new JsonObject {
                ["id"] = userId,
                ["name"] = userName,
                ["avatar"] = userAvatar,
                ["accountType"] = userAccountType,
                ["class"] = userClass,
                ["banner"] = userBanner
            },
            ["message"] = message,
            ["date"] = date,
            ["deleted"] = deleted,
            ["replyingToUuid"] = replyingToUuid
        };

        // cenzura: studentum se schovava class
        if (!(client?.AccountType <= Account.AccountType.STUDENT)) return obj;
        if (obj["author"]?["class"] is not null) obj["author"]!["class"] = null;

        return obj;
    }

    private static async Task SafeCloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason, CancellationToken ct) {
        try { await socket.CloseAsync(status, reason, ct); } catch { /* ignore */ }
    }

    private async Task RegisterHeartbeatAsync() {
        if (Interlocked.CompareExchange(ref heartbeatRegistered, 1, 0) == 0) {
            hub.RegisterHeartbeat("chat", async (hub2, token) => {
                await BroadcastConnectedUsersAsync(token);
            });
        }
    }
}