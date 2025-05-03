using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using MySql.Data.MySqlClient;
using Client = EduchemLP.Server.Classes.Objects.WSClientUser;

namespace EduchemLP.Server.Services;
/*
 * V pripade ze se uzivatel pripoji do socketu, tak se mu zobrazi poslednich 10 sprav
 * Aby backend prijmal zpravy a v pripade ze se ta sprava posle, tak se zobrazi vsem ostatnim
 */


public static class WSChat {
    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
    private static readonly List<Client> ConnectedUsers = [];
    //private static Timer? statusTimer;



    //handle 
    public static async Task HandleQueueAsync(WebSocket webSocket) {
        User? sessionAccount = await Auth.ReAuthUserAsync();
        if (sessionAccount == null) {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unauthorized", CancellationToken.None);
            return;
        }
        
        var client = new Client(
            webSocket,
            sessionAccount.ID,
            sessionAccount.DisplayName,
            sessionAccount.AccountType,
            sessionAccount.Class,
            sessionAccount.Avatar,
            sessionAccount.Banner
        );



        // zjisteni duplikatu
        lock (ConnectedUsers) {
            var list = ConnectedUsers.ToList();

            foreach (var connectedClient in list.Where(connectedClient => connectedClient.ID == client.ID)) {
                connectedClient.Disconnect();
                ConnectedUsers.Remove(connectedClient);
            }
        }




        lock(ConnectedUsers) ConnectedUsers.Add(client);
        client.SendInicialChat().Wait();
        SendConnectedUsersStatus().Wait();



        // zpracovani prijmutych zprav
        while (webSocket.State == WebSocketState.Open) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            } catch (WebSocketException) {
                break;
            }

            // zpracovani zpravy
            string messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            if (string.IsNullOrWhiteSpace(messageString)) continue;

            JsonNode? messageJson;
            try {
                messageJson = JsonNode.Parse(messageString);
            } catch (JsonException) {
                continue;
            }

            var action = messageJson?["action"]?.ToString();
            if (action == null) continue;

            switch (action) {
                case "sendMessage": {
                    var messageText = messageJson?["message"]?.ToString();
                    if (string.IsNullOrWhiteSpace(messageText))
                        break;

                    var savedMessage = await SaveMessageToDb(client, messageText);
                    if (savedMessage == null) {
                        await client.BroadcastAsync(new JsonObject {
                            ["action"] = "error",
                            ["message"] = "Chyba při ukládání zprávy do databáze."
                        }.ToString());
                        break;
                    }

                    var messageJsonString = savedMessage.ToString();

                    lock (ConnectedUsers) foreach (var connectedClient in ConnectedUsers) {
                        connectedClient.BroadcastAsync(messageJsonString).Wait();
                    }
                } break;
                
                case "deleteMessage": {
                    var messageUuid = messageJson?["uuid"]?.ToString();
                    if (string.IsNullOrWhiteSpace(messageUuid))
                        break;

                    await DeleteMessage(client, messageUuid);
                } break;
                
                case "loadOlderMessages": {
                    var beforeUuid = messageJson?["beforeUuid"]?.ToString();
                    if (string.IsNullOrWhiteSpace(beforeUuid)) break;

                    await SendOlderMessages(client, beforeUuid);
                } break;
            }
        }
        


        // pri ukonceni socketu
        lock (ConnectedUsers) ConnectedUsers.Remove(client);
        _ = SendConnectedUsersStatus();
    }
    

    
    //logisticky metody
    private static async Task DeleteMessage(Client client, string messageUuid) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return;

        // kontrola jestli uzivatel ma pravo smazat zpravu jinych uzivatelu 
        
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE chat SET deleted = 1 WHERE uuid = @uuid";
        cmd.Parameters.AddWithValue("@uuid", messageUuid);
        cmd.Parameters.AddWithValue("@userId", client.ID);

        var affectedRows = await cmd.ExecuteNonQueryAsync();
        if (affectedRows > 0) {
            
            var deleteMessageJson = new JsonObject {
                ["action"] = "deleteMessage",
                ["uuid"] = messageUuid
            };

            lock (ConnectedUsers) {
                foreach (var connectedClient in ConnectedUsers) {
                    connectedClient.BroadcastAsync(deleteMessageJson.ToString()).Wait();
                }
            }
        }
    }
    
    private static async Task<bool> SendInicialChat(this Client client) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
        """
            SELECT 
                c.*, 
                u.display_name as author_name, 
                u.avatar as author_avatar,
                u.class as author_class,
                u.account_type as author_account_type,
                u.banner as author_banner
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.deleted = 0
            ORDER BY `date` DESC LIMIT 20
        """;
        
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return false;
        var messages = new JsonArray();
        while (await reader.ReadAsync()) {
            if(reader.GetValueOrNull<int>("user_id") == null || reader.GetStringOrNull("author_name") == null) continue;

            var message = CreateMessageObject(
                reader.GetString("uuid"),
                reader.GetInt32("user_id"),
                reader.GetString("author_name"),
                reader.GetStringOrNull("author_avatar"),
                reader.GetString("author_account_type"),
                reader.GetStringOrNull("author_class"),
                reader.GetString("message"),
                reader.GetDateTime("date"),
                reader.GetStringOrNull("author_banner"),
                client
            );

            messages.Add(message);
        }

        await client.BroadcastAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages
        }.ToString());
        return true;
    }

    private static async Task<JsonObject?> SaveMessageToDb(Client client, string message) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;
        await using var cmd = conn.CreateCommand();
    
        var uuid = Guid.NewGuid().ToString();
        cmd.CommandText =
            """
                INSERT INTO chat (uuid, user_id, message, date, deleted)
                VALUES (@uuid, @userId, @message, NOW(), 0);

                SELECT 
                    c.*, 
                    u.display_name as author_name, 
                    u.avatar as author_avatar,
                    u.class as author_class,
                    u.account_type as author_account_type,
                    u.banner as author_banner
                FROM chat c
                LEFT JOIN users u ON c.user_id = u.id
                WHERE c.uuid = @uuid
            """;
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@userId", client.ID);
        cmd.Parameters.AddWithValue("@message", message);

        MySqlDataReader? result;

        try {
            result = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        } catch (MySqlException) {
            return null;
        }

        if (result == null) return null;

        if (!await result.ReadAsync()) return null;
        if(result.GetValueOrNull<int>("user_id") == null || result.GetStringOrNull("author_name") == null) return null;

        var user = new {
            ID = result.GetInt32("user_id"),
            DisplayName = result.GetString("author_name"),
            Avatar = result.GetStringOrNull("author_avatar")
        };

        return new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = new JsonArray {
                CreateMessageObject(
                    uuid,
                    user.ID,
                    user.DisplayName,
                    user.Avatar,
                    result.GetString("author_account_type"),
                    result.GetStringOrNull("author_class"),
                    message,
                    result.GetDateTime("date"),
                    result.GetStringOrNull("author_banner"),
                    client
                )
            }
        };
    }

    private static JsonObject CreateMessageObject(string uuid, int userId, string userName, string? userAvatar, string userAccountType, string? userClass, string message, DateTime date, string? userBanner, Client? client = null) {
        var obj = new JsonObject {
            ["uuid"] = uuid,
            ["author"] = new JsonObject {
                ["id"] = userId,
                ["name"] = userName,
                ["avatar"] = userAvatar,
                ["accountType"] = userAccountType,
                ["class"] = userClass,
                ["banner"] = userBanner,
            },
            ["message"] = message,
            ["date"] = date,
            ["deleted"] = deleted
        };

        // cenzura veci
        if(client?.AccountType <= User.UserAccountType.STUDENT) {
            if(obj["author"]?["class"] != null) obj["author"]!["class"] = null;
        }

        return obj;
    }

    private static async Task SendOlderMessages(Client client, string beforeUuid) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return;

        await using var dateCmd = conn.CreateCommand();
        dateCmd.CommandText = "SELECT `date` FROM chat WHERE uuid = @uuid";
        dateCmd.Parameters.AddWithValue("@uuid", beforeUuid);

        var beforeDateObj = await dateCmd.ExecuteScalarAsync();
        if (beforeDateObj is not DateTime beforeDate) return;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT 
                c.*, 
                u.display_name as author_name, 
                u.avatar as author_avatar,
                u.class as author_class,
                u.account_type as author_account_type,
                u.banner as author_banner
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.date < @beforeDate AND c.deleted = 0
            ORDER BY c.date DESC
            LIMIT 20
            """;

        cmd.Parameters.AddWithValue("@beforeDate", beforeDate);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return;

        var messages = new JsonArray();
        while (await reader.ReadAsync()) {
            if(reader.GetValueOrNull<int>("user_id") == null || reader.GetStringOrNull("author_name") == null) continue;

            var message = CreateMessageObject(
                reader.GetString("uuid"),
                reader.GetInt32("user_id"),
                reader.GetString("author_name"),
                reader.GetStringOrNull("author_avatar"),
                reader.GetString("author_account_type"),
                reader.GetStringOrNull("author_class"),
                reader.GetString("message"),
                reader.GetDateTime("date"),
                reader.GetStringOrNull("author_banner"),
                client
            );

            messages.Add(message);
        }

        // pokud neni co poslat (konec zprav), posle se info ze je konec zprav
        if (messages.Count == 0) {
            await client.BroadcastAsync(new JsonObject {
                ["action"] = "noMoreMessagesToFetch",
            }.ToString());

            return;
        }

        // jinak se poslou zpravy
        await client.BroadcastAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages,
            ["isLoadMoreAction"] = true,
        }.ToString());
    }

    private static async Task SendConnectedUsersStatus() {
        lock (ConnectedUsers) {
            var users = ConnectedUsers.Select(client => new {
                id = client.ID,
                name = client.DisplayName,
                avatar = client.Avatar
            }).ToList();

            var payload = new {
                action = "updateConnectedUsers",
                users
            };

            var json = JsonSerializer.Serialize(payload, JSON_OPTIONS);

            foreach (var client in ConnectedUsers)
                client.BroadcastAsync(json).Wait();
        }
    }

    /*static WSChat() {
        statusTimer = new Timer(Status!, null, 0, 1000);
    }*/
}