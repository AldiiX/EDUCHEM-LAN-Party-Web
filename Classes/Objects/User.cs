using System.Text.Json.Serialization;
using EduchemLPR.Services;
using MySql.Data.MySqlClient;

namespace EduchemLPR.Classes.Objects;




public class User {

    [JsonConstructor]
    private User(uint id, string displayName, string? email, string? @class, string authKey, string accountType) {
        ID = id;
        DisplayName = displayName;
        Class = @class;
        Email = email;
        AuthKey = authKey;
        AccountType = accountType;
    }

    public uint ID { get; private set; }
    public string DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string? Class { get; private set; }
    public string AuthKey { get; private set; }
    public string AccountType { get; private set; }



    public static async Task<User?> GetByAuthKeyAsync(string authKey) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand(@"SELECT * FROM users WHERE auth_key = @authKey", conn);
        cmd.Parameters.AddWithValue("@authKey", authKey);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return null;

        if (!await reader.ReadAsync()) return null;

        return new User(
            reader.GetUInt32("id"),
            reader.GetString("display_name"),
            reader.GetObjectOrNull("email") as string,
            reader.GetObjectOrNull("class") as string,
            reader.GetString("auth_key"),
            reader.GetString("account_type")
        );
    }

    public static User? GetByAuthKey(string authKey) => GetByAuthKeyAsync(authKey).Result;

    public static async Task<User?> AuthAsync(string key) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        await using var cmd = new MySqlCommand($"SELECT * FROM `users` WHERE `auth_key` = @key", conn);
        cmd.Parameters.AddWithValue("@key", key);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var acc = new User(
            reader.GetUInt32("id"),
            reader.GetString("display_name"),
            reader.GetObjectOrNull("email") as string,
            reader.GetObjectOrNull("class") as string,
            reader.GetString("auth_key"),
            reader.GetString("account_type")
        );

        HttpContextService.Current.Session.SetObject("loggeduser", acc);
        HttpContextService.Current.Items["loggeduser"] = acc;
        return acc;
    }

    public static User? Auth(in string key) => AuthAsync(key).Result;
}