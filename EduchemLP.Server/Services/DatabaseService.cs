using MySqlConnector;

namespace EduchemLP.Server.Services;

public class DatabaseService(MySqlDataSource dataSource) : IDatabaseService {
    public MySqlConnection? GetOpenConnection() {
        var conn = dataSource.CreateConnection();
        conn.Open();
        UseUtcSession(conn);
        return conn;
    }

    public async ValueTask<MySqlConnection?> GetOpenConnectionAsync(CancellationToken ct = default) {
        var conn = await dataSource.OpenConnectionAsync(ct);
        await UseUtcSessionAsync(conn, ct);
        return conn;
    }

    private static void UseUtcSession(MySqlConnection conn) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SET time_zone = '+00:00'";
        cmd.ExecuteNonQuery();
    }

    private static async Task UseUtcSessionAsync(MySqlConnection conn, CancellationToken ct) {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SET time_zone = '+00:00'";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
