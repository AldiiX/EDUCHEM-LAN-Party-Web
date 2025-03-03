using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Services;





public static class WSReservations {

    // classy
    public class Client {
        public int ID { get; set; }
        public string DisplayName { get; set; }
        public WebSocket WebSocket { get; set; }

        public Client(int id, string displayName, WebSocket webSocket) {
            ID = id;
            DisplayName = displayName;
            WebSocket = webSocket;
        }
    }

    // promenne
    private static readonly List<Client> ConnectedUsers = [];
    private static Timer? statusTimer;

    static WSReservations() {
        statusTimer = new Timer(Status!, null, 0, 1000);
    }





    // handle
    public static async Task HandleQueueAsync(WebSocket webSocket) {
        User? sessionAccount = await Auth.ReAuthUserAsync();

        var client = new Client(
            sessionAccount?.ID ?? new Random().Next(10000, int.MaxValue),
            sessionAccount?.DisplayName ?? "Unknown",
            webSocket
        );




        lock(ConnectedUsers) ConnectedUsers.Add(client);
        client.SendFullReservationInfoAsync().Wait();




        // zpracovani prijmutych zprav
        while (webSocket.State == WebSocketState.Open) {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            try {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            } catch (WebSocketException) {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close) {
                lock (ConnectedUsers) {
                    ConnectedUsers.Remove(client);
                }

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                return;
            }

            // zpracovani zpravy
            var messageJson = JsonNode.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count));

            var action = messageJson?["action"]?.ToString();
            if (action == null) continue;

            switch (action) {
                case "status":
                    await client.SendFullReservationInfoAsync();
                    break;
            }
        }



        // pri ukonceni socketu
        lock (ConnectedUsers) ConnectedUsers.Remove(client);
    }





    // metody
    private static void Status(object state) {
        lock (ConnectedUsers) {
            var cu = ConnectedUsers.ToList();

            foreach (var user in cu) {
                // pokud uzivatel neni pripojen
                if (user.WebSocket.State != WebSocketState.Open) {
                    user.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
                    ConnectedUsers.Remove(user);
                }

                BroadcastMessageAsync(user, "status").Wait();
            }
        }

    }



    // helper metody
    private static async Task SendMessageEndCloseAsync(WebSocket webSocket, string message, WebSocketCloseStatus closeStatus) {
        try {
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
        } catch (Exception) {
            // ignored
        } finally {
            await webSocket.CloseAsync(closeStatus, "", CancellationToken.None);
        }
    }

    private static async Task BroadcastMessageAsync(Client client, string message) {
        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = new List<Task>();

        client.WebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<bool> SendFullReservationInfoAsync(this Client client) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;

        var loggedUser = Utilities.GetLoggedAccountFromContextOrNull();

        var command = new MySqlCommand(
        """
                SELECT 
                    res.*, 
                    usr.id AS user_id, 
                    usr.display_name AS user_display_name, 
                    usr.class AS user_class,
                    COALESCE(room.id, NULL) AS room_id, 
                    COALESCE(room.limit_of_seats, NULL) AS room_limit, 
                    COALESCE(room.available, NULL) AS room_available,
                    COALESCE(comp.id, NULL) AS computer_id,
                    COALESCE(comp.is_teachers_pc, NULL) AS computer_is_teachers_pc,
                    COALESCE(comp.available, NULL) AS computer_available
                FROM reservations res
                LEFT JOIN users usr ON res.user_id = usr.id
                LEFT JOIN rooms room ON res.room_id = room.id
                LEFT JOIN computers comp ON res.computer_id = comp.id;
                """, conn
        );

        await using var reader = await command.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return false;

        var array = new JsonArray();
        while (await reader.ReadAsync()) {
            var obj = new JsonObject {
                ["user"] = loggedUser != null ? reader.GetValueOrNull<int>("user_id") != null ? new JsonObject() {
                    ["id"] = reader.GetValueOrNull<int>("user_id"),
                    ["displayName"] = reader.GetStringOrNull("user_display_name"),
                    ["class"] = reader.GetStringOrNull("user_class")
                } : null : "unknown",

                ["room"] = reader.GetValueOrNull<int>("room_id") != null ? new JsonObject() {
                    ["id"] = reader.GetValueOrNull<int>("room_id"),
                    ["limit"] = reader.GetValueOrNull<int>("room_limit"),
                    ["available"] = reader.GetValueOrNull<bool>("room_available")
                } : null,

                ["computer"] = reader.GetValueOrNull<int>("computer_id") != null ? new JsonObject() {
                    ["id"] = reader.GetValueOrNull<int>("computer_id"),
                    ["isTeachersPC"] = reader.GetValueOrNull<bool>("computer_is_teachers_pc"),
                    ["available"] = reader.GetValueOrNull<bool>("computer_available")
                } : null,

                ["note"] = reader.GetStringOrNull("note"),
                ["createdAt"] = reader.GetDateTime("created_at"),
            };

            array.Add(obj);
        }
        
        
        var payload = new {
            action = "fetchAll",
            data = array
        };

        var message = JsonSerializer.SerializeToUtf8Bytes(payload);
        await client.WebSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, CancellationToken.None);

        return true;
    }
}