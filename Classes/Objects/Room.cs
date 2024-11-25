using System.Text.Json;
using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes.Objects;




public class Room {

    private Room(string id, UInt16 limitOfSeats, List<int> reservedBy, List<string> reservedByName, List<string> reservedByClass) {
        ID = id;
        LimitOfSeats = limitOfSeats;
        ReservedBy = reservedBy;
        ReservedByName = reservedByName;
        ReservedByClass = reservedByClass;
    }

    public string ID { get; private set; }
    public UInt16 LimitOfSeats { get; private set; }
    public List<int> ReservedBy { get; private set; }
    public List<string> ReservedByName { get; private set; }
    public List<string> ReservedByClass { get; private set; }


    public static async Task<List<Room>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(@"
            SELECT
                r.id AS room_id,
                r.limit_of_seats,
                r.reserved_by,
                JSON_ARRAYAGG(u.display_name) AS reserved_by_display_names,
                JSON_ARRAYAGG(u.class) AS reserved_by_classes
            FROM
                rooms r
            LEFT JOIN JSON_TABLE(
                r.reserved_by,
                '$[*]' COLUMNS(user_id INT PATH '$')
            ) AS reserved
            ON TRUE
            LEFT JOIN users u 
            ON reserved.user_id = u.id
            GROUP BY
                r.id, r.limit_of_seats;
        ", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return [];

        var rooms = new List<Room>();
        while (await reader.ReadAsync()) {
            List<object> reservedByOriginal = reader.GetObjectOrNull("reserved_by") != null ? JsonSerializer.Deserialize<List<object>>(reader.GetString("reserved_by")) ?? [] : [];
            List<int> reservedBy = [];
            foreach (var id in reservedByOriginal) if (int.TryParse(id.ToString(), out var parsedId)) reservedBy.Add(parsedId);


            rooms.Add(new Room(
                reader.GetString("room_id"),
                reader.GetUInt16("limit_of_seats"),
                reservedBy,
                reader.GetObjectOrNull("reserved_by") != null ? JsonSerializer.Deserialize<List<string>>(reader.GetString("reserved_by_display_names")) ?? [] : [],
                reader.GetObjectOrNull("reserved_by") != null ? JsonSerializer.Deserialize<List<string>>(reader.GetString("reserved_by_classes")) ?? [] : []
            ));
        }

        return rooms;
    }

    public static List<Room> GetAll() => GetAllAsync().Result;
}