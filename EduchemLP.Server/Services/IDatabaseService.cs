namespace EduchemLP.Server.Services;

public interface IDatabaseService {
    MySqlConnector.MySqlConnection? GetOpenConnection();

    ValueTask<MySqlConnector.MySqlConnection?> GetOpenConnectionAsync(CancellationToken ct = default);
}