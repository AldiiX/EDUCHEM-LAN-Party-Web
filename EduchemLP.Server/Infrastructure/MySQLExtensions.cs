using MySqlConnector;

namespace EduchemLP.Server.Infrastructure;

public static class MySQLExtensions {
    public static T? GetValueOrNull<T>(this MySqlDataReader reader, string key) where T : struct {
        int ordinal = reader.GetOrdinal(key);
        if(reader.IsDBNull(ordinal)) return null;

        object value = reader.GetValue(ordinal);
        if(value is T result) return result;

        try {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception) {
            return null;
        }
    }

    public static string? GetStringOrNull(this MySqlDataReader reader, string key) {
        try {
            int ordinal = reader.GetOrdinal(key);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        } catch (Exception) {
            return null;
        }
    }
}