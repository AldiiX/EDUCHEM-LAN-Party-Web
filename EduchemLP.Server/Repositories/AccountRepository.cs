using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Controllers;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using MySqlConnector;

namespace EduchemLP.Server.Repositories;





public class AccountRepository(
    IDatabaseService db,
    IHttpContextAccessor http
) : IAccountRepository {

    public async Task<Account?> GetByIdAsync(int id, CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
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

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var user = new Account(reader);

        return user;
    }

    public async Task<List<Account>> GetAllAsync(CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return [];

        await using var cmd = new MySqlCommand(
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
            ORDER BY `display_name` ASC
        """, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Account>();
        while (await reader.ReadAsync(ct)) {
            list.Add(new Account(reader));
        }

        return list;
    }

    public async Task UpdateLastLoggedInAsync(Account account, CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return;

        const string updateQuery = "UPDATE users SET last_logged_in = @now WHERE id = @id";
        await using var cmd = new MySqlCommand(updateQuery, conn);
        cmd.Parameters.AddWithValue("@now", DateTime.Now);
        cmd.Parameters.AddWithValue("@id", account.Id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAvatarByConnectedPlatformAsync(Account account, CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return;

        // novy avatar
        string? newAvatarLink = null;

        // zjisteni veci podle Discordu
        Account.AccountAccessToken? discordToken = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD);
        Account.AccountAccessToken? googleToken  = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE);
        Account.AccountAccessToken? githubToken  = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GITHUB);

        // google
        if (googleToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateGoogleAccessTokenAsync(account, ct);

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await client.SendAsync(userInfoRequest, ct);
            var userInfo = JsonNode.Parse(await userInfoResponse.Content.ReadAsStringAsync(ct));

            //Console.WriteLine("Google User Info: " + userInfo?.ToJsonString());
            newAvatarLink = userInfo?["picture"]?.ToString();
        }

        // github
        else if (githubToken != null) {
            var client = new HttpClient();
            var accessToken = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GITHUB)?.AccessToken;
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", "EDUCHEM LAN Party");

            var response = await client.GetAsync("https://api.github.com/user", ct);
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
            //Console.WriteLine("UpdateAvatarByConnectedPlatform: " + content?.ToJsonString());

            newAvatarLink = content?["avatar_url"]?.ToString();
        }

        // discord
        else if (discordToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateDiscordAccessTokenAsync(account, ct);
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://discord.com/api/users/@me", ct);
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
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
        cmd.Parameters.AddWithValue("@id", account.Id);
        cmd.Parameters.AddWithValue("@avatar", newAvatarLink);
        await cmd.ExecuteNonQueryAsync(ct);



        // aktualizace avataru v session
        var sessionAcc = JsonNode.Parse(http.HttpContext!.Session.GetString("loggedaccount") ?? "");
        if (sessionAcc == null) return;

        sessionAcc.AsObject()["avatar"] = newAvatarLink;
        http.HttpContext.Session.SetString("loggedaccount", sessionAcc.ToJsonString());
    }

    public async Task<Account?> CreateAsync(string email, string displayName, string? @class, Account.AccountGender gender, Account.AccountType accountType, bool sendToEmail = false, bool enableReservation = false, CancellationToken ct = default) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return null;

        var password = Utilities.GenerateRandomPassword();
        const string insertQuery =
            """
            INSERT INTO users (email, display_name, password, class, account_type, gender, enable_reservation) VALUES (@email, @displayName, @password, @class, @accountType, @gender, @enableReservation);

            SELECT * FROM users WHERE id = LAST_INSERT_ID();
            """;
        await using var cmd = new MySqlCommand(insertQuery, conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@displayName", displayName);
        cmd.Parameters.AddWithValue("@password", Utilities.EncryptPassword(password));
        cmd.Parameters.AddWithValue("@class", @class);
        cmd.Parameters.AddWithValue("@accountType", accountType.ToString().ToUpper());
        cmd.Parameters.AddWithValue("@gender", gender.ToString().ToUpper());
        cmd.Parameters.AddWithValue("@enableReservation", enableReservation ? 1 : 0);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        var user = new Account(reader);



        // odeslání emailu
        if (sendToEmail) {
            string webLink = "https://" + Program.ROOT_DOMAIN + "/api/v1/lg?u=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email + " " + password));
            _ = EmailService.SendHTMLEmailAsync(user.Email, "Registrace do EDUCHEM LAN Party", "~/Views/Emails/UserRegistered.cshtml",
                new EmailUserRegisterModel(password, webLink, user.Email)
            );
        }



        return user;
    }

    public async Task<string?> GenerateDiscordAccessTokenAsync(Account account, CancellationToken ct = default) {
        var discordAccessToken = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD);
        if (discordAccessToken?.RefreshToken is null) return null;

        using var client = new HttpClient();
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return null;

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", discordAccessToken.AccessToken);

        // zjištění platnosti tokenu
        var testRequest = await client.GetAsync("https://discord.com/api/users/@me", ct);
        //Console.WriteLine(testRequest.ToJsonString());

        // když je token neplatný - přegeneruje se
        if (testRequest.StatusCode != System.Net.HttpStatusCode.Unauthorized) return testRequest.IsSuccessStatusCode ? discordAccessToken.AccessToken : null;

        var refreshClient = new HttpClient();
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");

        refreshRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["DISCORD_CLIENT_ID"] },
            { "client_secret", Program.ENV["DISCORD_CLIENT_SECRET"] },
            { "grant_type", "refresh_token" },
            { "refresh_token", discordAccessToken.RefreshToken },
            { "redirect_uri", DiscordOAuthController.REDIRECT_URI }
        });

        var refreshResponse = await refreshClient.SendAsync(refreshRequest, ct);
        var refreshContent = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync(ct));
        //Console.WriteLine("Token refresh response: " + refreshContent?.ToJsonString());

        // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
        if (refreshContent?["access_token"] == null || refreshContent["refresh_token"] == null) {
            const string deleteQuery = "DELETE FROM users_access_tokens WHERE user_id = @userId AND platform = @platform";
            await using var deleteCmd = new MySqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@userId", account.Id);
            deleteCmd.Parameters.AddWithValue("@platform", nameof(Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD).ToUpper());
            await deleteCmd.ExecuteNonQueryAsync(ct);

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
        cmd.Parameters.AddWithValue("@userId", account.Id);
        cmd.Parameters.AddWithValue("@platform", nameof(Account.AccountAccessToken.Platform.DISCORD).ToUpper());
        cmd.Parameters.AddWithValue("@accessToken", refreshContent["access_token"]?.ToString());
        cmd.Parameters.AddWithValue("@refreshToken", refreshContent["refresh_token"]?.ToString());
        await cmd.ExecuteNonQueryAsync(ct);

        return refreshContent["access_token"]?.ToString();

    }

    public async Task<string?> GenerateGoogleAccessTokenAsync(Account account, CancellationToken ct = default) {
        var googleAccessToken = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE);
        if (googleAccessToken?.RefreshToken is null) return null;

        using var client = new HttpClient();
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return null;

        // Zkusíme volat Google API, abychom zjistili, jestli access token ještě platí
        var testRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        testRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", googleAccessToken.AccessToken);

        var testResponse = await client.SendAsync(testRequest, ct);

        if (testResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
            var refreshClient = new HttpClient();
            var refreshContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "client_id", Program.ENV["GOOGLE_CLIENT_ID"] },
                { "client_secret", Program.ENV["GOOGLE_CLIENT_SECRET"] },
                { "grant_type", "refresh_token" },
                { "refresh_token", googleAccessToken.RefreshToken! }
            });

            var refreshResponse = await refreshClient.PostAsync("https://oauth2.googleapis.com/token", refreshContent, ct);
            var refreshData = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync(ct));
            //Console.WriteLine("Google token refresh response: " + refreshData?.ToJsonString());

            // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
            if (refreshData?["access_token"] == null) {
                const string deleteQuery = "DELETE FROM users_access_tokens WHERE user_id = @userId AND platform = @platform";
                await using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@userId", account.Id);
                deleteCmd.Parameters.AddWithValue("@platform", nameof(Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE).ToUpper());
                await deleteCmd.ExecuteNonQueryAsync(ct);

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
            cmd.Parameters.AddWithValue("@userId", account.Id);
            cmd.Parameters.AddWithValue("@platform", nameof(Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE).ToUpper());
            cmd.Parameters.AddWithValue("@accessToken", newAccessToken);
            await cmd.ExecuteNonQueryAsync(ct);

            return newAccessToken;
        }

        if (testResponse.IsSuccessStatusCode)
            return googleAccessToken.AccessToken;

        // Jiná chyba při ověřování access tokenu
        //Console.WriteLine("Unexpected status code when checking Google token: " + testResponse.StatusCode);
        return null;
    }
}