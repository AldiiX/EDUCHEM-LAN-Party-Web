using System.Text.Json.Nodes;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EduchemLP.Server.Controllers;


[Route("/_be/instagram/oauth")]
public class InstagramOAuthController(
    IAuthService auth,
    IDatabaseService db,
    IAccountRepository accounts
) : Controller {

    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/instagram/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/instagram/oauth";
    #endif

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string code, CancellationToken ct = default) {
        var account = await auth.ReAuthFromContextOrNullAsync(ct);
        if (account == null) return RedirectPermanent("/login");


        var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["INSTAGRAM_CLIENT_ID"] },
            { "client_secret", Program.ENV["INSTAGRAM_CLIENT_SECRET"] },
            { "grant_type", "authorization_code" },
            { "redirect_uri", DiscordOAuthController.REDIRECT_URI },
            { "code", code }
        });

        var response = await client.PostAsync("https://api.instagram.com/oauth/access_token", content, ct);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));

        //Console.WriteLine("Instagram OAuth Response: " + body?.ToJsonString());

        var accessToken = body?["access_token"]?.ToString();
        var userId = body?["user_id"]?.ToString();

        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return RedirectPermanent("/app/account");

        await using var cmd = new MySqlCommand(
            """
            DELETE FROM users_access_tokens WHERE platform = 'INSTAGRAM' AND user_id = @userId;

            INSERT INTO 
                users_access_tokens (user_id, platform, access_token, refresh_token, token_type) 
                VALUES (@userId, 'INSTAGRAM', @accessToken, null, 'BEARER');
            """,
            conn);

        cmd.Parameters.AddWithValue("@userId", account.Id );
        cmd.Parameters.AddWithValue("@accessToken", body?["access_token"]?.ToString());
        await cmd.ExecuteNonQueryAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return RedirectPermanent("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account");
    }
}