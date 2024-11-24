using System.Text.Json;
using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes.Objects;




public class Room {
    private Room(string id, UInt16 limitOfSeats, List<uint> reservedBy) {
        ID = id;
        LimitOfSeats = limitOfSeats;
        ReservedBy = reservedBy;
    }

    public string ID { get; private set; }
    public UInt16 LimitOfSeats { get; private set; }
    public List<uint> ReservedBy { get; private set; }


    public static async Task<List<Room>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(@"SELECT * FROM rooms", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return [];

        var rooms = new List<Room>();
        while (await reader.ReadAsync()) {
            rooms.Add(new Room(
                reader.GetString("id"),
                reader.GetUInt16("limit_of_seats"),
                reader.GetObjectOrNull("reserved_by") != null ? JsonSerializer.Deserialize<List<uint>>(reader.GetString("reserved_by")) ?? [] : []
            ));
        }

        return rooms;
    }

    public static List<Room> GetAll() => GetAllAsync().Result;
}