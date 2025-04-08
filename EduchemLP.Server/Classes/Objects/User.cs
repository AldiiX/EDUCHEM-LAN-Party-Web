using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




public class User {
    public enum UserGender { MALE, FEMALE, OTHER}

    public enum UserAccountType {
        STUDENT,
        TEACHER,
        ADMIN,
        SUPERADMIN,
    }


    [JsonConstructor]
    private User(int id, string displayName, string email, string password, string? @class, UserAccountType accountType, DateTime lastUpdated/*, UserGender? gender*/, string? avatar) {
        ID = id;
        DisplayName = displayName;
        Class = @class;
        Password = password;
        Email = email;
        AccountType = accountType;
        LastUpdated = lastUpdated;
        Avatar = avatar;
        //Gender = gender;
    }


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
        const string query = "SELECT * FROM `users` WHERE `id` = @id";
        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(
            reader.GetInt32("id"),
            reader.GetString("display_name"),
            reader.GetString("email"),
            reader.GetString("password"),
            reader.GetObjectOrNull("class") as string,
            Enum.TryParse(reader.GetString("account_type"), out UserAccountType _ac) ? _ac : UserAccountType.STUDENT,
            reader.GetDateTime("last_updated"),
            //Enum.TryParse<UserGender>(reader.GetObjectOrNull
            reader.GetStringOrNull("avatar")
        );

        return user;
    }

    public static async Task<User?> AuthAsync(string email, string hashedPassword) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        const string query = "SELECT * FROM `users` WHERE `email` = @email AND `password` = @password";
        await using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@password", hashedPassword);

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(
            reader.GetInt32("id"),
            reader.GetString("display_name"),
            reader.GetString("email"),
            reader.GetString("password"),
            reader.GetObjectOrNull("class") as string,
            Enum.TryParse(reader.GetString("account_type"), out UserAccountType _ac) ? _ac : UserAccountType.STUDENT,
            reader.GetDateTime("last_updated"),
            //Enum.TryParse<UserGender>(reader.GetObjectOrNull
            reader.GetStringOrNull("avatar")
        );

        // aktualizace posledního přihlášení
        _ = UpdateLastLoggedInAsync(user.ID);

        // dalsi nastaveni
        var httpContext = HttpContextService.Current;
        httpContext.Session.SetObject("loggeduser", user);
        httpContext.Items["loggeduser"] = user;

        return user;
    }

    public static User? Auth(in string email, in string hashedPassword) => AuthAsync(email, hashedPassword).Result;

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
                reader.GetString("email"),
                reader.GetString("password"),
                reader.GetStringOrNull("class"),
                Enum.TryParse(reader.GetString("account_type"), out UserAccountType _ac) ? _ac : UserAccountType.STUDENT,
                reader.GetDateTime("last_updated"),
                //Enum.TryParse<UserGender>(reader.GetObject
                reader.GetStringOrNull("avatar")
            ));
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

    public static User? Create(string email, string displayName, string? @class, UserGender gender, string accountType, bool sendToEmail = false) => CreateAsync(email, displayName, @class, gender, accountType, sendToEmail).Result;

    public static async Task<User?> CreateAsync(string email, string displayName, string? @class, UserGender gender, string accountType, bool sendToEmail = false) {
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
        cmd.Parameters.AddWithValue("@accountType", accountType);
        cmd.Parameters.AddWithValue("@gender", gender.ToString().ToUpper());

        await using var reader = await cmd.ExecuteReaderAsync() as MySqlDataReader;
        if (reader == null || !reader.Read()) return null;

        var user = new User(
            reader.GetInt32("id"),
            reader.GetString("display_name"),
            reader.GetString("email"),
            reader.GetString("password"),
            reader.GetObjectOrNull("class") as string,
            Enum.TryParse(reader.GetString("account_type"), out UserAccountType _ac) ? _ac : UserAccountType.STUDENT,
            reader.GetDateTime("last_updated"),
            //Enum.TryParse<UserGender>(reader.GetObjectOrNull
            reader.GetStringOrNull("avatar")
        );



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