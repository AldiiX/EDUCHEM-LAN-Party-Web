using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Data;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLP.Server.Controllers;

[Route("/_be/github/oauth")]
public class GitHubOAuthController(
    IAuthService auth,
    EduchemLpDbContext db,
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
        if (account == null) return Redirect("/login");

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

        // kontrola odpovedi (pokud request byl zrusen, tak se nic neposle do db)
        if (accessToken == null || error != null) {
            return Redirect("/app/account?tab=settings");
        }

        // zapsani do db
        var token = await db.AccountAccessTokens.FindAsync(
            [Account.AccountAccessToken.AccountAccessTokenPlatform.GITHUB, account.Id],
            cancellationToken: ct
        );

        if (token == null) {
            db.AccountAccessTokens.Add(new Account.AccountAccessToken(
                userId: account.Id,
                platform: Account.AccountAccessToken.AccountAccessTokenPlatform.GITHUB,
                accessToken: accessToken,
                refreshToken: refreshToken,
                type: Account.AccountAccessToken.AccountAccessTokenType.BEARER
            ));
        } else {
            token.AccessToken = accessToken;
            token.RefreshToken = refreshToken;
            token.Type = Account.AccountAccessToken.AccountAccessTokenType.BEARER;
        }

        await db.SaveChangesAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return Redirect("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account?tab=settings");
    }
}
