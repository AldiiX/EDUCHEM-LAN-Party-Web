using System.Text.Json;
using System.Text.Json.Serialization;
using EduchemLP.Server.Services;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




public class User {

    [JsonConstructor]
    private User(int id, string displayName, string? email, string? @class, string authKey, string accountType, DateTime lastUpdated) {
        ID = id;
        DisplayName = displayName;
        Class = @class;
        Email = email;
        AuthKey = authKey;
        AccountType = accountType;
        LastUpdated = lastUpdated;
    }

    public int ID { get; private set; }
    public string DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? Class { get; private set; }
    public string AuthKey { get; private set; }
    public string AccountType { get; private set; }
    public DateTime LastUpdated { get; private set; }



    public static async Task<User?> GetByAuthKeyAsync(string authKey) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand(@"SELECT * FROM users WHERE auth_key = @authKey", conn);
        cmd.Parameters.AddWithValue("@authKey", authKey);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return null;

        if (!await reader.ReadAsync()) return null;

        return new User(
            reader.GetInt32("id"),
            reader.GetString("display_name"),
            reader.GetObjectOrNull("email") as string,
            reader.GetObjectOrNull("class") as string,
            reader.GetString("auth_key"),
            reader.GetString("account_type"),
            reader.GetDateTime("last_updated")
        );
    }

    public static User? GetByAuthKey(string authKey) => GetByAuthKeyAsync(authKey).Result;

    public static async Task<User?> AuthAsync(string key) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        const string query = "SELECT * FROM `users` WHERE `auth_key` = @key";
        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@key", key);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(
            reader.GetInt32("id"),
            reader.GetString("display_name"),
            reader.GetObjectOrNull("email") as string,
            reader.GetObjectOrNull("class") as string,
            reader.GetString("auth_key"),
            reader.GetString("account_type"),
            reader.GetDateTime("last_updated")
        );

        // aktualizace posledního přihlášení
        _ = UpdateLastLoggedInAsync(user.AuthKey);

        // dalsi nastaveni
        var httpContext = HttpContextService.Current;
        httpContext.Session.SetObject("loggeduser", user);
        httpContext.Items["loggeduser"] = user;

        return user;
    }

    public static User? Auth(in string key) => AuthAsync(key).Result;

    public static async Task<List<User?>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return new List<User?>();

        await using var cmd = new MySqlCommand(@"SELECT * FROM users", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return new List<User?>();

        var list = new List<User?>();
        while (await reader.ReadAsync()) {
            list.Add(new User(
                reader.GetInt32("id"),
                reader.GetString("display_name"),
                reader.GetObjectOrNull("email") as string,
                reader.GetObjectOrNull("class") as string,
                reader.GetString("auth_key"),
                reader.GetString("account_type"),
                reader.GetDateTime("last_updated")
            ));
        }

        return list;
    }

    public static List<User?> GetAll() => GetAllAsync().Result;

    private static async Task UpdateLastLoggedInAsync(string authKey) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return;

        const string updateQuery = "UPDATE users SET last_logged_in = @now WHERE auth_key = @authKey";
        await using var cmd = new MySqlCommand(updateQuery, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.Now);
        cmd.Parameters.AddWithValue("@authKey", authKey);
        await cmd.ExecuteNonQueryAsync();
    }

    public override string ToString() {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}