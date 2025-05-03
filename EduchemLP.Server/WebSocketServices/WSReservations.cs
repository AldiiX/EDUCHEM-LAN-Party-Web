using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using MySql.Data.MySqlClient;
using Client = EduchemLP.Server.Classes.Objects.WSClientUser;

namespace EduchemLP.Server.WebSocketServices;


public static class WSReservations {

    
    // promenne
    private static readonly List<Client> ConnectedClients = [];
    private static Timer? statusTimer;
    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static WSReservations() {
        statusTimer = new Timer(Status!, null, 0, 60 * 1000);
    }





    // handle
    public static async Task HandleQueueAsync(WebSocket webSocket) {
        User? sessionAccount = await Auth.ReAuthUserAsync();

        var client = new Client(
            webSocket,
            sessionAccount?.ID ?? new Random().Next(10_000, 999_999),
            sessionAccount?.DisplayName ?? "Guest",
            sessionAccount?.AccountType,
            sessionAccount?.Class,
            sessionAccount?.Avatar,
            sessionAccount?.Banner
        );



        lock(ConnectedClients) ConnectedClients.Add(client);
        client.SendFullReservationInfoAsync().Wait();
        Status(null!);




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
                case "reserve": {
                    if (!AppSettings.AreReservationsEnabledRightNow) break;

                    string? room = messageJson?["room"]?.ToString();
                    string? computer = messageJson?["computer"]?.ToString();

                    if (room == null && computer == null) break;

                    await using var conn = await Database.GetConnectionAsync();
                    if (conn == null) break;

                    var command = new MySqlCommand(
                        """
                               DELETE FROM reservations WHERE user_id = @user_id;
                               INSERT INTO reservations (user_id, room_id, computer_id) VALUES (@user_id, @room_id, @computer_id);
                               """, conn
                    );

                    command.Parameters.AddWithValue("@user_id", sessionAccount?.ID);
                    command.Parameters.AddWithValue("@room_id", room);
                    command.Parameters.AddWithValue("@computer_id", computer);

                    await command.ExecuteNonQueryAsync();
                    lock (ConnectedClients) {
                        foreach (var c in ConnectedClients) {
                            c.SendFullReservationInfoAsync().Wait();
                        }
                    }

                    // lognuti rezervace
                    _ = DbLogger.LogAsync(DbLogger.LogType.INFO, $"Uživatel {sessionAccount?.DisplayName} ({sessionAccount?.Email}) rezervoval {room ?? computer}.", "reservation");
                } break;

                case "deleteReservation": {
                    if (!AppSettings.AreReservationsEnabledRightNow) break;

                    await using var conn = await Database.GetConnectionAsync();
                    if (conn == null) break;

                    var command = new MySqlCommand(
                    """
                           DELETE FROM reservations WHERE user_id = @user_id;
                           """, conn
                    );

                    command.Parameters.AddWithValue("@user_id", sessionAccount?.ID);

                    await command.ExecuteNonQueryAsync();
                    lock (ConnectedClients) {
                        foreach (var c in ConnectedClients) {
                            c.SendFullReservationInfoAsync().Wait();
                        }
                    }

                    // lognuti
                    _ = DbLogger.LogAsync(DbLogger.LogType.INFO, $"Uživatel {sessionAccount?.DisplayName} ({sessionAccount?.Email}) zrušil rezervaci.", "reservation");
                } break;

                case "disconnect": {
                    await client.DisconnectAsync("Closed by user");
                } break;
            }
        }



        // pri ukonceni socketu
        lock (ConnectedClients) ConnectedClients.Remove(client);
        Status(null!);
    }





    // metody
    private static void Status(object state) {
        lock (ConnectedClients) {
            var clients = ConnectedClients.ToList();

            foreach (var client in clients) {
                // pokud uzivatel neni pripojen
                if (client.WebSocket.State != WebSocketState.Open) {
                    client.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
                    ConnectedClients.Remove(client);
                }

                // connectedUsers zasifrovani dat
                var connectedUsers = new JsonArray();

                foreach (var c in clients.DistinctBy(c => c.ID).ToList()) {
                    if (c.AccountType == null) {
                        connectedUsers.Add("unknown");
                        continue;
                    }

                    connectedUsers.Add(new JsonObject {
                        ["id"] = c.ID,
                        ["displayName"] = c.DisplayName,
                        ["class"] = c.AccountType > User.UserAccountType.STUDENT ? c.Class : null,
                    });
                }

                var message = JsonSerializer.Serialize(new {
                    action = "status",
                    connectedUsers = connectedUsers,
                }, JsonSerializerOptions.Web);

                BroadcastMessageAsync(client, message).Wait();
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
        if (client.WebSocket is not { State: WebSocketState.Open }) return;


        var buffer = Encoding.UTF8.GetBytes(message);
        var tasks = new List<Task>();

        client.WebSocket?.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task<bool> SendFullReservationInfoAsync(this Client client) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;

        var loggedUser = Utilities.GetLoggedAccountFromContextOrNull();
        var computers = Computer.GetAllAsync();
        var rooms = Room.GetAllAsync();

        var command = new MySqlCommand(
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
                    ["class"] = loggedUser.AccountType > User.UserAccountType.STUDENT ? reader.GetStringOrNull("user_class") : null,
                    ["avatar"] = reader.GetStringOrNull("user_avatar"),
                    ["banner"] = reader.GetStringOrNull("user_banner"),
                } : null : "unknown",

                ["room"] = reader.GetStringOrNull("room_id") != null ? new JsonObject() {
                    ["id"] = reader.GetString("room_id"),
                    ["label"] = reader.GetStringOrNull("room_label") ?? reader.GetString("room_id"),
                    ["limitOfSeats"] = reader.GetValueOrNull<int>("room_limit"),
                    ["available"] = reader.GetValueOrNull<bool>("room_available"),
                    ["image"] = reader.GetStringOrNull("room_image"),
                } : null,

                ["computer"] = reader.GetStringOrNull("computer_id") != null ? new JsonObject() {
                    ["id"] = reader.GetStringOrNull("computer_id"),
                    ["isTeachersPC"] = reader.GetValueOrNull<bool>("computer_is_teachers_pc"),
                    ["available"] = reader.GetValueOrNull<bool>("computer_available"),
                    ["image"] = reader.GetStringOrNull("computer_image"),
                } : null,

                ["note"] = reader.GetStringOrNull("note"),
                ["createdAt"] = reader.GetDateTime("created_at"),
            };

            array.Add(obj);
        }
        
        
        var payload = new {
            action = "fetchAll",
            reservations = array,
            computers = computers.Result,
            rooms = rooms.Result,
        };

        var message = JsonSerializer.Serialize(payload, JSON_OPTIONS);
        BroadcastMessageAsync(client, message).Wait();

        return true;
    }
}