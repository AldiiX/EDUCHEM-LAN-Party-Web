using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Services;
using MySqlConnector;

namespace EduchemLP.Server.Repositories;



public class RoomRepository(IDatabaseService db) : IRoomRepository {

    public async Task<List<Room>> GetAllAsync(CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(@"
            SELECT * FROM rooms WHERE available = 1
        ", conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var rooms = new List<Room>();
        while (await reader.ReadAsync(ct)) {
            rooms.Add(new Room(
                reader.GetString("id"),
                reader.GetString("label"),
                reader.GetStringOrNull("image"),
                reader.GetUInt16("limit_of_seats")
            ));
        }

        return rooms;
    }
}