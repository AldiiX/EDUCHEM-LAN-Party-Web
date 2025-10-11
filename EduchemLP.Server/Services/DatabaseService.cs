using MySqlConnector;

namespace EduchemLP.Server.Services;

public class DatabaseService(MySqlDataSource dataSource) : IDatabaseService {
    public MySqlConnection? GetOpenConnection() {
        var conn = dataSource.CreateConnection();
        conn.Open();
        return conn;
    }

    public async ValueTask<MySqlConnection?> GetOpenConnectionAsync(CancellationToken ct = default) {
        return await dataSource.OpenConnectionAsync(ct);
    }
}
