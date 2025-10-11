using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EduchemLP.Server.Controllers;

[Route("/_be/github/oauth")]
public class GitHubOAuthController(
    IAuthService auth,
    IDatabaseService db,
    IAccountRepository accounts
) : Controller {
    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/github/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/github/oauth";
    #endif

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string code, [FromQuery] string? state, CancellationToken ct = default) {
        var account = await auth.ReAuthFromContextOrNullAsync(ct);
        if (account == null) return RedirectPermanent("/login");

        var client = new HttpClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["GITHUB_CLIENT_ID"] },
            { "client_secret", Program.ENV["GITHUB_CLIENT_SECRET"] },
            { "code", code },
            { "redirect_uri", REDIRECT_URI }
        });

        var response = await client.PostAsync("https://github.com/login/oauth/access_token", content, ct);
        var responseString = await response.Content.ReadAsStringAsync(ct);

        // Parse responseString jako query string
        var queryParams = System.Web.HttpUtility.ParseQueryString(responseString);
        //Console.WriteLine(queryParams.ToJsonString());

        var accessToken = queryParams["access_token"];
        var refreshToken = queryParams["refresh_token"]; // většinou NULL (GitHub OAuth apps defaultně nevrací refresh token)
        var error = queryParams["error"];

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(accessToken)){
            //Console.WriteLine("GitHub OAuth Error: " + responseString);
            return RedirectPermanent("/login");
        }

        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return RedirectPermanent("/app/account");

        await using var cmd = new MySqlCommand(
        """
        DELETE FROM users_access_tokens WHERE platform = 'GITHUB' AND user_id = @userId;

        INSERT INTO 
            users_access_tokens (user_id, platform, access_token, refresh_token, token_type) 
            VALUES (@userId, 'GITHUB', @accessToken, @refreshToken, 'BEARER');
        """,
        conn);
        cmd.Parameters.AddWithValue("@userId", account.Id);
        cmd.Parameters.AddWithValue("@accessToken", accessToken);
        cmd.Parameters.AddWithValue("@refreshToken", refreshToken ?? "");
        await cmd.ExecuteNonQueryAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return RedirectPermanent("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account?tab=settings");
    }
}
