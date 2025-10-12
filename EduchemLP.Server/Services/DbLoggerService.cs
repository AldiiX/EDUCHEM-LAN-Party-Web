using MySqlConnector;

namespace EduchemLP.Server.Services;

public class DbLoggerService(IDatabaseService db) : IDbLoggerService {
    public async Task<bool> LogAsync(IDbLoggerService.LogType type, string message, string exactType = "basic", CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return false;

        await using var cmd = new MySqlCommand("INSERT INTO logs (type, exact_type, message, date) VALUES (@type, @exacttype, @message, NOW())", conn);
        cmd.Parameters.AddWithValue("@type", type.ToString().ToUpper());
        cmd.Parameters.AddWithValue("@exacttype", exactType);
        cmd.Parameters.AddWithValue("@message", message); 

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> LogErrorAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.ERROR, message, exactType, ct);

    public async Task<bool> LogInfoAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.INFO, message, exactType, ct);

    public async Task<bool> LogWarnAsync(string message, string exactType = "basic", CancellationToken ct = default) => await LogAsync(IDbLoggerService.LogType.WARN, message, exactType, ct);
}