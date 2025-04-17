using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using MySql.Data.MySqlClient;

/*
 * V pripade ze se uzivatel pripoji do socketu, tak se mu zobrazi poslednich 10 sprav
 * Aby backend prijmal zpravy a v pripade ze se ta sprava posle, tak se zobrazi vsem ostatnim
 */

namespace EduchemLP.Server.Services;

public static class WSChat {
    private static readonly List<WSClient> ConnectedUsers = [];
    private static Timer? statusTimer;

    //handle 
    public static async Task HandleQueueAsync(WebSocket webSocket) {
        User? sessionAccount = await Auth.ReAuthUserAsync();
        if (sessionAccount == null) {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Unauthorized", CancellationToken.None);
            return;
        }
        
        var client = new WSClient(
            sessionAccount.ID,
            sessionAccount.DisplayName,
            webSocket,
            sessionAccount.AccountType.ToString().ToUpper(),
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

                    var savedMessage = await SaveMessageToDb(client.ID, messageText);
                    if (savedMessage == null)
                        break;

                    var messageJsonString = savedMessage.ToString();

                    lock (ConnectedUsers) foreach (var connectedClient in ConnectedUsers) {
                        connectedClient.BroadcastMessageAsync(messageJsonString).Wait();
                    }
                } break;
            }
        }
        


        // pri ukonceni socketu
        lock (ConnectedUsers) ConnectedUsers.Remove(client);
    }
    

    //metodiky 
    private static async Task BroadcastMessageAsync(this WSClient client, string message) {
        if (client.WebSocket is not { State: WebSocketState.Open }) return;


        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = new List<Task>();

        client.WebSocket?.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
    //logisticky metody
    private static async Task<bool> SendInicialChat(this WSClient client) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
                          SELECT c.*, u.display_name as author_name, u.avatar as author_avatar
                          FROM chat c
                          LEFT JOIN users u ON c.user_id = u.id
                          ORDER BY `date` DESC LIMIT 20
                          """;
        
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return false;
        var messages = new JsonArray();
        while (await reader.ReadAsync()) {
            if(reader.GetValueOrNull<int>("user_id") == null || reader.GetStringOrNull("author_name") == null) continue;

            var message = new JsonObject {
                ["uuid"] = reader.GetString("uuid"),
                ["author"] = new JsonObject {
                    ["id"] = reader.GetInt32("user_id"),
                    ["name"] = reader.GetString("author_name"),
                    ["avatar"] = reader.GetStringOrNull("author_avatar")
                },
                ["message"] = reader.GetString("message"),
                ["date"] = reader.GetDateTime("date")
            };

            messages.Add(message);
        }

        await client.BroadcastMessageAsync(new JsonObject {
            ["action"] = "sendMessages",
            ["messages"] = messages
        }.ToString());
        return true;
    }

    private static async Task<JsonObject?> SaveMessageToDb(int userId, string message) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;
        await using var cmd = conn.CreateCommand();
    
        var uuid = Guid.NewGuid().ToString();
        cmd.CommandText =
            """
                INSERT INTO chat (uuid, user_id, message, date)
                VALUES (@uuid, @userId, @message, NOW());

                SELECT c.*, u.display_name as author_name, u.avatar as author_avatar
                FROM chat c
                LEFT JOIN users u ON c.user_id = u.id
                WHERE c.uuid = @uuid
            """;
        cmd.Parameters.AddWithValue("@uuid", uuid);
        cmd.Parameters.AddWithValue("@userId", userId);
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
                new JsonObject {
                    ["uuid"] = uuid,
                    ["author"] = new JsonObject {
                        ["id"] = userId,
                        ["name"] = user.DisplayName,
                        ["avatar"] = user.Avatar
                    },
                    ["message"] = message,
                    ["date"] = DateTime.Now
                }
            }
        };
    }
    /*static WSChat() {
        statusTimer = new Timer(Status!, null, 0, 1000);
    }*/
}