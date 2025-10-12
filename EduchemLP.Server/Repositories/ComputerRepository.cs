using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Services;
using MySqlConnector;

namespace EduchemLP.Server.Repositories;

public class ComputerRepository(IDatabaseService db) : IComputerRepository {

    public async Task<List<Computer>> GetAllAsync(CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
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

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct)) {
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
}