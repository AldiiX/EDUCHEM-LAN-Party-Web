using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




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
                r.*,
                GROUP_CONCAT(reserved_users.id ORDER BY reserved_users.created_at) AS reserved_by,
                GROUP_CONCAT(reserved_users.display_name ORDER BY reserved_users.created_at) AS reserved_by_display_names,
                GROUP_CONCAT(
                    COALESCE(reserved_users.class, 'null')
                    ORDER BY reserved_users.created_at
                    SEPARATOR ','
                ) AS reserved_by_classes
            FROM rooms r
            LEFT JOIN (
                SELECT 
                    rv.room_id,
                    u.id,
                    u.display_name,
                    u.class,
                    rv.created_at
                FROM reservations rv
                JOIN users u ON rv.user_id = u.id
            ) AS reserved_users ON r.id = reserved_users.room_id
            WHERE r.available = 1
            GROUP BY r.id
            ORDER BY r.id;
        ", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return [];

        var rooms = new List<Room>();
        while (await reader.ReadAsync()) {
            List<int> reservedBy = [];
            if (reader.GetObjectOrNull("reserved_by") != null)
                reservedBy.AddRange(reader.GetString("reserved_by").Split(',').Select(int.Parse));

            List<string> reservedByName = reader.GetObjectOrNull("reserved_by_display_names") != null ? reader.GetString("reserved_by_display_names").Split(',').ToList() : [];

            List<string?> reservedByClass = (reader.GetObjectOrNull("reserved_by_classes") != null ? reader.GetString("reserved_by_classes").Split(',').ToList() : [])!;
            for (var i = 0; i < reservedByClass.Count; i++) {
                if (reservedByClass[i] == "null") reservedByClass[i] = null;
            }

            rooms.Add(new Room(
                reader.GetString("id"),
                reader.GetUInt16("limit_of_seats"),
                reservedBy,
                reservedByName,
                reservedByClass
            ));
        }

        return rooms;
    }

    public static List<Room> GetAll() => GetAllAsync().Result;
}