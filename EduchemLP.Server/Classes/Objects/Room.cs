using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




public class Room {

    private Room(string id, string label, string? image, UInt16 limitOfSeats) {
        ID = id;
        Label = label;
        Image = image;
        LimitOfSeats = limitOfSeats;
    }

    public string ID { get; private set; }
    public string Label { get; private set; }
    public string? Image { get; private set; }
    public UInt16 LimitOfSeats { get; private set; }



    public static async Task<List<Room>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(@"
            SELECT * FROM rooms WHERE available = 1
        ", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return [];

        var rooms = new List<Room>();
        while (await reader.ReadAsync()) {
            rooms.Add(new Room(
                reader.GetString("id"),
                reader.GetString("label"),
                reader.GetObjectOrNull("image") as string,
                reader.GetUInt16("limit_of_seats")
            ));
        }

        return rooms;
    }

    public static List<Room> GetAll() => GetAllAsync().Result;
}