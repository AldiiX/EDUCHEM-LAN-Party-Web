using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes.Objects;





public class Computer {

    private Computer(string id, int? reservedBy, string? reservedByName, string? reservedByClass, bool isTeacherPC) {
        ID = id;
        ReservedBy = reservedBy;
        ReservedByName = reservedByName;
        ReservedByClass = reservedByClass;
        IsTeacherPC = isTeacherPC;
    }

    public string ID { get; private set; }
    public int? ReservedBy { get; private set; }
    public string? ReservedByName { get; private set; }
    public string? ReservedByClass { get; private set; }
    public bool IsTeacherPC { get; private set; }

    public static async Task<List<Computer>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        var computers = new List<Computer>();
        await using var cmd = new MySqlCommand(@"
            SELECT c.*, 
                   r.user_id AS reserved_by,
                   u.display_name AS reserved_by_name,
                   u.class AS reserved_by_class
            FROM computers c
            LEFT JOIN reservations r on c.id = r.computer_id
            LEFT JOIN users u ON r.user_id = u.id
            WHERE c.available = 1
        ", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return computers;

        while (await reader.ReadAsync()) {
            computers.Add(
                new Computer(
                    reader.GetString("id"),
                    reader.GetObjectOrNull("reserved_by") as int?,
                    reader.GetObjectOrNull("reserved_by_name") as string,
                    reader.GetObjectOrNull("reserved_by_class") as string,
                    reader.GetBoolean("is_teachers_pc")
                )
            );
        }

        return computers;
    }

    public static List<Computer> GetAll() => GetAllAsync().Result;
}