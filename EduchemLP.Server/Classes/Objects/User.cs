using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




public class User {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserGender { MALE, FEMALE, OTHER}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserAccountType {
        STUDENT,
        TEACHER,
        ADMIN,
        SUPERADMIN,
    }


    [JsonConstructor]
    private User(int id, string displayName, string email, string password, string? @class, UserAccountType accountType, DateTime lastUpdated, UserGender? gender, string? avatar) {
        ID = id;
        DisplayName = displayName;
        Class = @class;
        Password = password;
        Email = email;
        AccountType = accountType;
        LastUpdated = lastUpdated;
        Avatar = avatar;
        Gender = gender;
    }

    private User(MySqlDataReader reader) : this(
        reader.GetInt32("id"),
        reader.GetString("display_name"),
        reader.GetString("email"),
        reader.GetString("password"),
        reader.GetObjectOrNull("class") as string,
        Enum.TryParse(reader.GetString("account_type"), out UserAccountType _ac) ? _ac : UserAccountType.STUDENT,
        reader.GetDateTime("last_updated"),
        Enum.TryParse<UserGender>(reader.GetStringOrNull("gender"), out var _g ) ? _g : null,
        reader.GetStringOrNull("avatar"),
        JsonSerializer.Deserialize<List<UserAccessToken>>(reader.GetStringOrNull("access_tokens") ?? "[]", JsonSerializerOptions.Web) ?? []
    ){}



    public int ID { get; private set; }
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }
    public string? Class { get; private set; }
    public string? Avatar { get; private set; }
    public UserAccountType AccountType { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public UserGender? Gender { get; private set; }



    public static User? GetById(in int id) => GetByIdAsync(id).Result;

    public static async Task<User?> GetByIdAsync(int id) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        const string query =
            """
                SELECT 
                    u.*,
                    COALESCE(
                        (
                            SELECT JSON_ARRAYAGG(
                                JSON_OBJECT(
                                    'userId', at.user_id,
                                    'platform', at.platform,
                                    'accessToken', at.access_token,
                                    'refreshToken', at.refresh_token,
                                    'type', at.token_type
                                )
                            )
                            FROM users_access_tokens at
                            WHERE at.user_id = u.id
                        ),
                        JSON_ARRAY()
                    ) AS access_tokens
                FROM `users` u
                WHERE `id` = @id
                LIMIT 1
            """;

        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(reader);

        return user;
    }

    public static async Task<User?> AuthAsync(string email, string plainPassword, bool updateUserByConnectedPlatforms = false) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        const string query =
            """
                SELECT 
                    u.*,
                    COALESCE(
                        (
                            SELECT JSON_ARRAYAGG(
                                JSON_OBJECT(
                                    'userId', at.user_id,
                                    'platform', at.platform,
                                    'accessToken', at.access_token,
                                    'refreshToken', at.refresh_token,
                                    'type', at.token_type
                                )
                            )
                            FROM users_access_tokens at
                            WHERE at.user_id = u.id
                        ),
                        JSON_ARRAY()
                    ) AS access_tokens
                FROM `users` u
                WHERE `email` = @email
                LIMIT 1
            """;

        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@email", email);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(reader);

        // overeni hesla - pokud je spatne tak null
        if (!Utilities.VerifyPassword(plainPassword, user.Password)) return null;


        // aktualizace picovin
        _ = UpdateLastLoggedInAsync(user.ID);
        //if (updateUserByConnectedPlatforms) _ = user.UpdateAvatarByConnectedPlatform();

        // nastavení do kontextu
        var httpContext = HttpContextService.Current;
        httpContext.Session.SetObject("loggeduser", user);
        httpContext.Items["loggeduser"] = user;

        return user;
    }


    public static User? Auth(in string email, in string plainPassword, in bool updateUserByConnectedPlatforms = false) => AuthAsync(email, plainPassword, updateUserByConnectedPlatforms).Result;

    public static async Task<List<User?>> GetAllAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(@"SELECT * FROM users", conn);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null) return [];

        var list = new List<User?>();
        while (await reader.ReadAsync()) {
            list.Add(new User(reader));
        }

        return list;
    }

    public static List<User?> GetAll() => GetAllAsync().Result;

    private static async Task UpdateLastLoggedInAsync(int id) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return;

        const string updateQuery = "UPDATE users SET last_logged_in = @now WHERE id = @id";
        await using var cmd = new MySqlCommand(updateQuery, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.Now);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public static User? Create(string email, string displayName, string? @class, UserGender gender, UserAccountType accountType, bool sendToEmail = false) => CreateAsync(email, displayName, @class, gender, accountType, sendToEmail).Result;

    public static async Task<User?> CreateAsync(string email, string displayName, string? @class, UserGender gender, UserAccountType accountType, bool sendToEmail = false) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        var password = Utilities.GenerateRandomPassword();
        const string insertQuery =
            """
            INSERT INTO users (email, display_name, password, class, account_type, gender) VALUES (@email, @displayName, @password, @class, @accountType, @gender);

            SELECT * FROM users WHERE id = LAST_INSERT_ID();
            """;
        await using var cmd = new MySqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@displayName", displayName);
        cmd.Parameters.AddWithValue("@password", Utilities.EncryptPassword(password));
        cmd.Parameters.AddWithValue("@class", @class);
        cmd.Parameters.AddWithValue("@accountType", accountType.ToString().ToUpper());
        cmd.Parameters.AddWithValue("@gender", gender.ToString().ToUpper());

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(reader);



        // odeslání emailu
        if (sendToEmail) {
            string webLink = "https://" + Program.ROOT_DOMAIN + "/api/v1/lg?u=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email + " " + password));
            _ = EmailService.SendHTMLEmailAsync(user.Email, "Registrace do EDUCHEM LAN Party", "~/Views/Emails/UserRegistered.cshtml",
                new EmailUserRegisterModel(password, webLink, user.Email)
            );
        }



        return user;
    }

    public override string ToString() {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}