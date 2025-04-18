using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
            sessionAccount.Class
        );




        lock(ConnectedUsers) ConnectedUsers.Add(client);
        client.SendInicialChat().Wait();



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
                    if (savedMessage == null)
                        break;

                    var messageJsonString = savedMessage.ToString();

                    lock (ConnectedUsers) foreach (var connectedClient in ConnectedUsers) {
                        connectedClient.BroadcastMessageAsync(messageJsonString).Wait();
                    }
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
    }
    

    //metodiky 
    private static async Task BroadcastMessageAsync(this Client client, string message) {
        if (client.WebSocket is not { State: WebSocketState.Open }) return;


        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = new List<Task>();

        client.WebSocket?.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
    //logisticky metody
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
              u.account_type as author_account_type
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
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
                client
            );

            messages.Add(message);
        }

        await client.BroadcastMessageAsync(new JsonObject {
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
                INSERT INTO chat (uuid, user_id, message, date)
                VALUES (@uuid, @userId, @message, NOW());

                SELECT 
                    c.*, 
                    u.display_name as author_name, 
                    u.avatar as author_avatar,
                    u.class as author_class,
                    u.account_type as author_account_type
                FROM chat c
                LEFT JOIN users u ON c.user_id = u.id
                WHERE c.uuid = @uuid
            """;
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@userId", client.ID);
        cmd.Parameters.AddWithValue("@message", message);

        var result = await cmd.ExecuteReaderAsync() as MySqlDataReader;
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
                    client
                )
            }
        };
    }

    private static JsonObject CreateMessageObject(string uuid, int userId, string userName, string? userAvatar, string userAccountType, string? userClass, string message, DateTime date, Client? client = null) {
        var obj = new JsonObject {
            ["uuid"] = uuid,
            ["author"] = new JsonObject {
                ["id"] = userId,
                ["name"] = userName,
                ["avatar"] = userAvatar,
                ["accountType"] = userAccountType,
                ["class"] = userClass,
            },
            ["message"] = message,
            ["date"] = date,
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
                u.account_type as author_account_type
            FROM chat c
            LEFT JOIN users u ON c.user_id = u.id
            WHERE c.date < @beforeDate
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
                client
            );

            messages.Add(message);
        }

        await client.BroadcastMessageAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages
        }.ToString());
    }

    /*static WSChat() {
        statusTimer = new Timer(Status!, null, 0, 1000);
    }*/
}