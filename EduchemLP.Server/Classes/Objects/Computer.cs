using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;





public class Computer {

    private Computer(string id, bool isTeacherPC, string? image) {
        ID = id;
        IsTeacherPC = isTeacherPC;
        Image = image;
    }

    public string ID { get; private set; }
    public string? Image { get; private set; }
    public bool IsTeacherPC { get; private set; }

    public static async Task<List<Computer>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        var computers = new List<Computer>();
        await using var cmd = new MySqlCommand(@"
            SELECT 
                c.*, 
                COALESCE(r.image, NULL) AS image
            FROM computers c
            LEFT JOIN rooms r ON c.room_id = r.id
            WHERE c.available = 1
        ", conn);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if(reader == null) return computers;

        while (await reader.ReadAsync()) {
            computers.Add(
                new Computer(
                    reader.GetString("id"),
                    reader.GetBoolean("is_teachers_pc"),
                    reader.GetStringOrNull("image")
                )
            );
        }

        return computers;
    }

    public static List<Computer> GetAll() => GetAllAsync().Result;
}