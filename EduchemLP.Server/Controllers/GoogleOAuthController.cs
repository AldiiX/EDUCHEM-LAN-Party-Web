using System.Text.Json.Nodes;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EduchemLP.Server.Controllers;

[Route("/_be/google/oauth")]
public class GoogleOAuthController(
    IAuthService auth,
    IDatabaseService db,
    IAccountRepository accounts
) : Controller {

    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/google/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/google/oauth";
    #endif

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string code, CancellationToken ct = default) {
        var account = await auth.ReAuthFromContextOrNullAsync(ct);
        if (account == null) return RedirectPermanent("/login");

        var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["GOOGLE_CLIENT_ID"] },
            { "client_secret", Program.ENV["GOOGLE_CLIENT_SECRET"] },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", REDIRECT_URI }
        });

        var response = await client.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        var tokenData = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));
        //Console.WriteLine("tokenData: " + tokenData?.ToJsonString());

        var accessToken = tokenData?["access_token"]?.ToString();
        var refreshToken = tokenData?["refresh_token"]?.ToString();



        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return RedirectPermanent("/app/account");

        await using var cmd = new MySqlCommand(
            """
            DELETE FROM users_access_tokens WHERE platform = 'GOOGLE' AND user_id = @userId;

            INSERT INTO 
                users_access_tokens (user_id, platform, access_token, refresh_token, token_type) 
                VALUES (@userId, 'GOOGLE', @accessToken, @refreshToken, 'BEARER');
            """,
            conn);
        cmd.Parameters.AddWithValue("@userId", account.Id );
        cmd.Parameters.AddWithValue("@accessToken", accessToken);
        cmd.Parameters.AddWithValue("@refreshToken", refreshToken);
        await cmd.ExecuteNonQueryAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return RedirectPermanent("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account?tab=settings");
    }
}