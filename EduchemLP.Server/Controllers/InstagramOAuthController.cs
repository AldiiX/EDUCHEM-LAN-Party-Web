using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Controllers;


[Route("/_be/instagram/oauth")]
public class InstagramOAuthController : Controller {

    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/instagram/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/instagram/oauth";
    #endif

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string code) {
        var account = Utilities.GetLoggedAccountFromContextOrNull();
        if (account == null) return RedirectPermanent("/login");


        var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["INSTAGRAM_CLIENT_ID"] },
            { "client_secret", Program.ENV["INSTAGRAM_CLIENT_SECRET"] },
            { "grant_type", "authorization_code" },
            { "redirect_uri", DiscordOAuthController.REDIRECT_URI },
            { "code", code }
        });

        var response = await client.PostAsync("https://api.instagram.com/oauth/access_token", content);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        //Console.WriteLine("Instagram OAuth Response: " + body?.ToJsonString());

        var accessToken = body?["access_token"]?.ToString();
        var userId = body?["user_id"]?.ToString();

        // zapsani do db
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return RedirectPermanent("/app/account");

        using var cmd = new MySqlCommand(
            """
            DELETE FROM users_access_tokens WHERE platform = 'INSTAGRAM' AND user_id = @userId;

            INSERT INTO 
                users_access_tokens (user_id, platform, access_token, refresh_token, token_type) 
                VALUES (@userId, 'INSTAGRAM', @accessToken, null, 'BEARER');
            """,
            conn);

        cmd.Parameters.AddWithValue("@userId", account.ID );
        cmd.Parameters.AddWithValue("@accessToken", body?["access_token"]?.ToString());
        cmd.ExecuteNonQuery();

        _ = account.UpdateAvatarByConnectedPlatform();

        return Redirect("/app/account");
    }
}