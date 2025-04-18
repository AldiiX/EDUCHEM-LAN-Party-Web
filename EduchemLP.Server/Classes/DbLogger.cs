using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes;

public static class DbLogger {


    public enum LogType { INFO, WARN, ERROR }

    public async static Task<bool> LogAsync(LogType type, string message, string exactType = "basic") {
        await using var conn = await Database.GetConnectionAsync();
        if(conn == null) return false;

        using var cmd = new MySqlCommand("INSERT INTO logs (type, exact_type, message, date) VALUES (@type, @exacttype, @message, NOW())", conn);
        cmd.Parameters.AddWithValue("@type", type.ToString().ToUpper());
        cmd.Parameters.AddWithValue("@exacttype", exactType);
        cmd.Parameters.AddWithValue("@message", message); 

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public static bool Log(LogType type, string message, string exactType = "basic") => LogAsync(type, message, exactType).Result;

    public static async Task<bool> LogErrorAsync(string message, string exactType = "basic") => await LogAsync(LogType.ERROR, message, exactType);

    public static bool LogError(string message, string exactType = "basic") => LogAsync(LogType.ERROR, message, exactType).Result;

    public static async Task<bool> LogInfoAsync(string message, string exactType = "basic") => await LogAsync(LogType.INFO, message, exactType);

    public static bool LogInfo(string message, string exactType = "basic") => LogAsync(LogType.INFO, message, exactType).Result;
}