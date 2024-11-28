using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes;





public static class Database {

    public static MySqlConnection? GetConnection() {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection($"server={(Program.USE_LOCALHOST_CONNECTION ? "localhost" : Program.ENV["DATABASE_IP"])};userid={Program.ENV["DATABASE_USERNAME"]};password={Program.ENV["DATABASE_PASSWORD"]};database={Program.ENV["DATABASE_DBNAME"]};pooling=true;Max Pool Size=300;");
            conn.Open();
        } catch (Exception e) {
            Program.Logger.LogCritical(e, "Failed to connect to the database.");
        }

        return conn;
    }

    public static async Task<MySqlConnection?> GetConnectionAsync() {
        MySqlConnection? conn = null;

        try {
            conn = new MySqlConnection($"server={(Program.USE_LOCALHOST_CONNECTION ? "localhost" : Program.ENV["DATABASE_IP"])};userid={Program.ENV["DATABASE_USERNAME"]};password={Program.ENV["DATABASE_PASSWORD"]};database={Program.ENV["DATABASE_DBNAME"]};pooling=true;Max Pool Size=300;");
            await conn.OpenAsync().ConfigureAwait(true);
        } catch (Exception e) {
            Program.Logger.LogCritical(e, "Failed to connect to the database.");
        }

        return conn;
    }

    public static async Task<object?> GetDataAsync(string property) {
        await using var conn = await GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand($"SELECT value FROM data WHERE property = @prop", conn);
        cmd.Parameters.AddWithValue("@prop", property);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !await reader.ReadAsync()) return null;

        return reader.GetObjectOrNull("value");
    }

    public static object? GetData(in string property) => GetDataAsync(property).Result;




    public static object? GetObjectOrNull(this MySqlDataReader reader, in string column) {
        return reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetValue(reader.GetOrdinal(column));
    }
}