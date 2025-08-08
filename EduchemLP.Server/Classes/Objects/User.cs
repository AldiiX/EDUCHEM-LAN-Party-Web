using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using EduchemLP.Server.Controllers;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes.Objects;




public partial class User {
    
    public int ID { get; private set; }
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }
    public string? Class { get; private set; }
    public string? Avatar { get; private set; }
    public string? Banner { get; private set; }
    public UserAccountType AccountType { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public UserGender? Gender { get; private set; }
    public List<UserAccessToken> AccessTokens { get; private set; } = [];

    public bool EnableReservation { get; private set; }

    [JsonConstructor]
    private User(int id, string displayName, string email, string password, string? @class, UserAccountType accountType, DateTime lastUpdated, UserGender? gender, string? avatar, string? banner, List<UserAccessToken>? accessTokens, bool enableReservation = false) {
        ID = id;
        DisplayName = displayName;
        Class = @class;
        Password = password;
        Email = email;
        AccountType = accountType;
        LastUpdated = lastUpdated;
        Avatar = avatar;
        Banner = banner;
        Gender = gender;
        AccessTokens = accessTokens ?? [];
        EnableReservation = enableReservation;
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
        reader.GetStringOrNull("banner"),
        JsonSerializer.Deserialize<List<UserAccessToken>>(reader.GetStringOrNull("access_tokens") ?? "[]", JsonSerializerOptions.Web) ?? [],
        reader.GetBoolean("enable_reservation")
    ){}



    
    


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
        if (updateUserByConnectedPlatforms) _ = user.UpdateAvatarByConnectedPlatformAsync();

        // nastavení do kontextu
        var httpContext = HttpContextService.Current;
        httpContext.Session.SetObject("loggeduser", user);
        httpContext.Items["loggeduser"] = user;

        //Console.WriteLine(user.ToJsonString());
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

    public async Task UpdateAvatarByConnectedPlatformAsync() {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return;

        // novy avatar
        string? newAvatarLink = null;

        // zjisteni veci podle Discordu
        UserAccessToken? discordToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.DISCORD);
        UserAccessToken? googleToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.GOOGLE);
        UserAccessToken? githubToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.GITHUB);

        // google
        if (googleToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateGoogleAccessTokenAsync();

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await client.SendAsync(userInfoRequest);
            var userInfo = JsonNode.Parse(await userInfoResponse.Content.ReadAsStringAsync());

            //Console.WriteLine("Google User Info: " + userInfo?.ToJsonString());
            newAvatarLink = userInfo?["picture"]?.ToString();
        }

        // github
        else if (githubToken != null) {
            var client = new HttpClient();
            var accessToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.GITHUB)?.AccessToken;
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", "EDUCHEM LAN Party");

            var response = await client.GetAsync("https://api.github.com/user");
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            //Console.WriteLine("UpdateAvatarByConnectedPlatform: " + content?.ToJsonString());

            newAvatarLink = content?["avatar_url"]?.ToString();
        }

        // discord
        else if (discordToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateDiscordAccessTokenAsync();
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://discord.com/api/users/@me");
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync());
            //Console.WriteLine("UpdateAvatarByConnectedPlatform: " + content?.ToJsonString());

            // ziskani avataru
            var avatarId = content?["avatar"]?.ToString();
            var avatarHash = content?["discriminator"]?.ToString();
            var accountId = content?["id"]?.ToString();

            if (avatarId != null && avatarHash != null) {
                newAvatarLink = "https://cdn.discordapp.com/avatars/" + accountId + "/" + avatarId + ".png?size=256";
            }
        }



        // updatnuti v db
        const string updateQuery =
            """
            UPDATE users 
            SET 
                avatar = IF(@avatar IS NULL, avatar, @avatar)
            WHERE id = @id
            """;
        await using var cmd = new MySqlCommand(updateQuery, conn);
        cmd.Parameters.AddWithValue("@id", ID);
        cmd.Parameters.AddWithValue("@avatar", newAvatarLink);
        await cmd.ExecuteNonQueryAsync();

        // aktualizace avataru v session
        await Classes.Auth.ReAuthUserAsync();
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





    // platformove veci
    private async Task<string?> GenerateDiscordAccessTokenAsync() {
        var discordAccessToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.DISCORD);
        if (discordAccessToken?.RefreshToken is null) return null;

        using var client = new HttpClient();
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", discordAccessToken.AccessToken);

        // zjištění platnosti tokenu
        var testRequest = await client.GetAsync("https://discord.com/api/users/@me");
        //Console.WriteLine(testRequest.ToJsonString());

        // když je token neplatný - přegeneruje se
        if (testRequest.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            var refreshClient = new HttpClient();
            var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");

            refreshRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", Program.ENV["DISCORD_CLIENT_ID"] },
                { "client_secret", Program.ENV["DISCORD_CLIENT_SECRET"] },
                { "grant_type", "refresh_token" },
                { "refresh_token", discordAccessToken.RefreshToken },
                { "redirect_uri", DiscordOAuthController.REDIRECT_URI }
            });

            var refreshResponse = await refreshClient.SendAsync(refreshRequest);
            var refreshContent = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync());
            //Console.WriteLine("Token refresh response: " + refreshContent?.ToJsonString());

            // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
            if (refreshContent == null || refreshContent["access_token"] == null || refreshContent["refresh_token"] == null) {
                const string deleteQuery = "DELETE FROM users_access_tokens WHERE user_id = @userId AND platform = @platform";
                await using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@userId", ID);
                deleteCmd.Parameters.AddWithValue("@platform", UserAccessToken.UserAccessTokenPlatform.DISCORD.ToString().ToUpper());
                await deleteCmd.ExecuteNonQueryAsync();

                return null;
            }


            // update v db
            const string updateQuery = """
                UPDATE users_access_tokens 
                SET 
                    access_token = @accessToken,
                    refresh_token = @refreshToken
                WHERE user_id = @userId AND platform = @platform
            """;

            await using var cmd = new MySqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@userId", ID);
            cmd.Parameters.AddWithValue("@platform", nameof(UserAccessToken.UserAccessTokenPlatform.DISCORD).ToUpper());
            cmd.Parameters.AddWithValue("@accessToken", refreshContent["access_token"]?.ToString());
            cmd.Parameters.AddWithValue("@refreshToken", refreshContent["refresh_token"]?.ToString());
            await cmd.ExecuteNonQueryAsync();

            return refreshContent["access_token"]?.ToString();
        }

        return testRequest.IsSuccessStatusCode ? discordAccessToken.AccessToken : null;
    }

    /*public async Task<string?> GenerateInstagramAccessTokenAsync() {
        var instagramAccessToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.INSTAGRAM);
        if (instagramAccessToken is null) return null;

        using var client = new HttpClient();

        // Zkusíme volat Instagram API, abychom zjistili, jestli access token ještě platí
        var testResponse = await client.GetAsync($"https://graph.instagram.com/me?access_token={instagramAccessToken.AccessToken}");

        if (testResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            // Token expirovaný nebo neplatný, pokusíme se o refresh
            Console.WriteLine("Instagram access token expired, refreshing...");

            var refreshClient = new HttpClient();
            var refreshUrl = $"https://graph.instagram.com/refresh_access_token" +
                             $"?grant_type=ig_refresh_token" +
                             $"&access_token={instagramAccessToken.AccessToken}";

            var refreshResponse = await refreshClient.GetAsync(refreshUrl);
            var refreshContent = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync());
            Console.WriteLine("Instagram token refresh response: " + refreshContent?.ToJsonString());

            if (refreshContent == null || refreshContent["access_token"] == null) {
                return null;
            }

            string newAccessToken = refreshContent["access_token"]?.ToString()!;

            // Update v DB
            const string updateQuery = """
                UPDATE users_access_tokens 
                SET 
                    access_token = @accessToken
                WHERE user_id = @userId AND platform = @platform
            """;

            await using var conn = await Database.GetConnectionAsync();
            if (conn == null) return null;

            await using var cmd = new MySqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@userId", ID);
            cmd.Parameters.AddWithValue("@platform", UserAccessToken.UserAccessTokenPlatform.INSTAGRAM.ToString().ToUpper());
            cmd.Parameters.AddWithValue("@accessToken", newAccessToken);
            await cmd.ExecuteNonQueryAsync();

            return newAccessToken;
        }

        if (testResponse.IsSuccessStatusCode)
            return instagramAccessToken.AccessToken;


        // Jiná chyba při ověřování access tokenu
        Console.WriteLine("Unexpected status code when checking Instagram token: " + testResponse.StatusCode);
        return null;
    }*/

    private async Task<string?> GenerateGoogleAccessTokenAsync() {
        var googleAccessToken = AccessTokens.FirstOrDefault(x => x.Platform == UserAccessToken.UserAccessTokenPlatform.GOOGLE);
        if (googleAccessToken?.RefreshToken is null) return null;

        using var client = new HttpClient();
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return null;

        // Zkusíme volat Google API, abychom zjistili, jestli access token ještě platí
        var testRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        testRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", googleAccessToken.AccessToken);

        var testResponse = await client.SendAsync(testRequest);

        if (testResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            var refreshClient = new HttpClient();
            var refreshContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", Program.ENV["GOOGLE_CLIENT_ID"] },
                { "client_secret", Program.ENV["GOOGLE_CLIENT_SECRET"] },
                { "grant_type", "refresh_token" },
                { "refresh_token", googleAccessToken.RefreshToken! }
            });

            var refreshResponse = await refreshClient.PostAsync("https://oauth2.googleapis.com/token", refreshContent);
            var refreshData = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync());
            //Console.WriteLine("Google token refresh response: " + refreshData?.ToJsonString());

            // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
            if (refreshData == null || refreshData["access_token"] == null) {
                const string deleteQuery = "DELETE FROM users_access_tokens WHERE user_id = @userId AND platform = @platform";
                await using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@userId", ID);
                deleteCmd.Parameters.AddWithValue("@platform", UserAccessToken.UserAccessTokenPlatform.GOOGLE.ToString().ToUpper());
                await deleteCmd.ExecuteNonQueryAsync();

                return null;
            }

            string newAccessToken = refreshData["access_token"]!.ToString();

            // Update v DB
            const string updateQuery = """
                UPDATE users_access_tokens 
                SET 
                    access_token = @accessToken
                WHERE user_id = @userId AND platform = @platform
            """;

            await using var cmd = new MySqlCommand(updateQuery, conn);
            cmd.Parameters.AddWithValue("@userId", ID);
            cmd.Parameters.AddWithValue("@platform", UserAccessToken.UserAccessTokenPlatform.GOOGLE.ToString().ToUpper());
            cmd.Parameters.AddWithValue("@accessToken", newAccessToken);
            await cmd.ExecuteNonQueryAsync();

            return newAccessToken;
        }

        if (testResponse.IsSuccessStatusCode)
            return googleAccessToken.AccessToken;

        // Jiná chyba při ověřování access tokenu
        //Console.WriteLine("Unexpected status code when checking Google token: " + testResponse.StatusCode);
        return null;
    }
}

public partial class User
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserGender { MALE, FEMALE, OTHER}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserAccountType {
        STUDENT,
        TEACHER,
        ADMIN,
        SUPERADMIN,
    }
}