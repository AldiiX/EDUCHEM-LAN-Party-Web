using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Controllers;


[Route("/_be/discord/oauth")]
public class DiscordOAuthController : Controller {

    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/discord/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/discord/oauth";
    #endif


    [HttpGet]
    public IActionResult Index([FromQuery] string code) {
        var account = Utilities.GetLoggedAccountFromContextOrNull();
        if (account == null) return RedirectPermanent("/login");



        // ziskani tokenu z codu
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
        request.Headers.Add("Accept", "application/json");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["DISCORD_CLIENT_ID"] },
            { "client_secret", Program.ENV["DISCORD_CLIENT_SECRET"] },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", REDIRECT_URI }
        });

        using var response = client.SendAsync(request).Result;
        var body = JsonNode.Parse(response.Content.ReadAsStringAsync().Result);


        // zapsani do db
        using var conn = Database.GetConnection();
        if (conn == null) return RedirectPermanent("/app/account");

        using var cmd = new MySqlCommand(
            """
                    DELETE FROM users_access_tokens WHERE platform = 'DISCORD' AND user_id = @userId;

                    INSERT INTO 
                        users_access_tokens (user_id, platform, access_token, refresh_token, token_type) 
                        VALUES (@userId, 'DISCORD', @accessToken, @refreshToken, 'BEARER');
                    """,
        conn);

        cmd.Parameters.AddWithValue("@userId", account.ID );
        cmd.Parameters.AddWithValue("@accessToken", body?["access_token"]?.ToString());
        cmd.Parameters.AddWithValue("@refreshToken", body?["refresh_token"]?.ToString());
        cmd.ExecuteNonQuery();

        _ = account.UpdateAvatarByConnectedPlatform();

        return RedirectPermanent("/app/account?tab=settings");
    }
}