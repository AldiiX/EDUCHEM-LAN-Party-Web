using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Data;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Services;
using Microsoft.EntityFrameworkCore;

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
    EduchemLpDbContext orm,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatWebSocketEndpoint> logger,
    IWebSocketHub hub
) : IWebSocketEndpoint {

    public PathString Path => "/ws/chat";

    private static int heartbeatRegistered = 0;

    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct) {
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var sessionUser = await auth.ReAuthAsync(ct);
        if (sessionUser is null) {
            await SafeCloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "Unauthorized", ct);
            return;
        }

        if (!await EnsureChatEnabled(socket, ct)) return;

        var client = new ChatClient(socket, sessionUser);
        var clients = hub.GetClients("chat").ToDictionary(c => c.Id, c => c);
        if (clients.TryGetValue(client.Id, out var existing)) {
            await existing.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Nové připojení z jiného zařízení.", ct);
            hub.RemoveClient("chat", client.Id);
        }

        hub.AddClient("chat", client);

        await SendInitialChatAsync(client, ct);
        await BroadcastConnectedUsersAsync(ct);
        await RegisterHeartbeatAsync();

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
                            await DeleteMessageAsync(client, uuid, ct);
                        }
                    }
                        break;

                    case "loadOlderMessages": {
                        var beforeUuid = messageJson?["beforeUuid"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(beforeUuid)) {
                            await SendOlderMessagesAsync(client, beforeUuid, ct);
                        }
                    }
                        break;
                }
            }
        }

        finally {
            hub.RemoveClient("chat", client.Id);

            try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); } catch { }

            await BroadcastConnectedUsersAsync(CancellationToken.None);
        }
    }

    private async Task<bool> EnsureChatEnabled(WebSocket socket, CancellationToken ct) {
        using var scope = scopeFactory.CreateScope();
        var appSettings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();

        if (await appSettings.GetChatEnabledAsync(ct)) return true;

        var clients = hub.GetClients("chat").ToDictionary(c => c.Id, c => c);
        foreach (var c in clients.Values) {
            await c.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Chat je vypnutý.", CancellationToken.None);
            hub.RemoveClient("chat", c.Id);
        }

        _ = SafeCloseAsync(socket, WebSocketCloseStatus.NormalClosure, "Chat je vypnutý.", ct);
        return false;
    }

    private async Task SendInitialChatAsync(ChatClient client, CancellationToken ct) {
        var messages = await orm.ChatMessages
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => !x.Deleted)
            .OrderByDescending(x => x.Date)
            .Take(20)
            .ToListAsync(ct);

        var arr = new JsonArray();

        foreach (var message in messages) {
            if (message.User is null) continue;

            arr.Add(CreateMessageObject(
                message.Uuid,
                message.UserId,
                message.User.DisplayName,
                message.User.Avatar,
                message.User.Type.ToString(),
                message.User.Class,
                message.Message,
                message.Date,
                message.User.Banner,
                message.Deleted,
                message.ReplyingToUuid,
                client
            ));
        }

        await client.SendAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = arr
        }.ToJsonString(JsonSerializerOptions.Web), ct);
    }

    private async Task<JsonObject?> SaveMessageToDbAsync(ChatClient client, string message, CancellationToken ct) {
        var uuid = Guid.NewGuid().ToString();

        var entity = new ChatMessage(uuid, (int)client.Id, message, null) {
            Date = DateTime.UtcNow,
            Deleted = false
        };

        orm.ChatMessages.Add(entity);
        await orm.SaveChangesAsync(ct);

        var account = await orm.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entity.UserId, ct);

        if (account is null) return null;

        return new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = new JsonArray {
                CreateMessageObject(
                    entity.Uuid,
                    entity.UserId,
                    account.DisplayName,
                    account.Avatar,
                    account.Type.ToString(),
                    account.Class,
                    entity.Message,
                    entity.Date,
                    account.Banner,
                    entity.Deleted,
                    entity.ReplyingToUuid,
                    client
                )
            }
        };
    }

    private async Task DeleteMessageAsync(ChatClient client, string messageUuid, CancellationToken ct) {
        var message = await orm.ChatMessages.FirstOrDefaultAsync(x => x.Uuid == messageUuid, ct);
        if (message is null) return;

        if (client.AccountType < Account.AccountType.TEACHER_ORG && message.UserId != client.Id) {
            await client.SendAsync(new JsonObject {
                ["action"] = "error",
                ["message"] = "Nemáte oprávnění smazat tuto zprávu."
            }.ToJsonString(JsonSerializerOptions.Web), ct);
            return;
        }

        message.Deleted = true;
        var affected = await orm.SaveChangesAsync(ct);

        if (affected > 0) {
            var payload = new JsonObject {
                ["action"] = "deleteMessage",
                ["uuid"] = messageUuid
            }.ToJsonString(JsonSerializerOptions.Web);

            await hub.BroadcastAsync("chat", payload, ct);
        }
    }

    private async Task SendOlderMessagesAsync(ChatClient client, string beforeUuid, CancellationToken ct) {
        var beforeDate = await orm.ChatMessages
            .AsNoTracking()
            .Where(x => x.Uuid == beforeUuid)
            .Select(x => (DateTime?)x.Date)
            .FirstOrDefaultAsync(ct);

        if (!beforeDate.HasValue) {
            return;
        }

        var messages = await orm.ChatMessages
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => !x.Deleted && x.Date < beforeDate.Value)
            .OrderByDescending(x => x.Date)
            .Take(20)
            .ToListAsync(ct);

        var arr = new JsonArray();

        foreach (var message in messages) {
            if (message.User is null) continue;

            arr.Add(CreateMessageObject(
                message.Uuid,
                message.UserId,
                message.User.DisplayName,
                message.User.Avatar,
                message.User.Type.ToString(),
                message.User.Class,
                message.Message,
                message.Date,
                message.User.Banner,
                message.Deleted,
                message.ReplyingToUuid,
                client
            ));
        }

        if (arr.Count == 0) {
            await client.SendAsync(new JsonObject { ["action"] = "noMoreMessagesToFetch" }.ToJsonString(JsonSerializerOptions.Web), ct);
            return;
        }

        await client.SendAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = arr,
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

        if (!(client?.AccountType <= Account.AccountType.STUDENT)) return obj;
        if (obj["author"]?["class"] is not null) obj["author"]!["class"] = null;

        return obj;
    }

    private static async Task SafeCloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason, CancellationToken ct) {
        try { await socket.CloseAsync(status, reason, ct); } catch { }
    }

    private async Task RegisterHeartbeatAsync() {
        if (Interlocked.CompareExchange(ref heartbeatRegistered, 1, 0) == 0) {
            hub.RegisterHeartbeat("chat", async (_, token) => {
                await BroadcastConnectedUsersAsync(token);
            });
        }
    }
}
