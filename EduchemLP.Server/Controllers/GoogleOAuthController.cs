using System.Text.Json.Nodes;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Data;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLP.Server.Controllers;

[Route("/_be/google/oauth")]
public class GoogleOAuthController(
    IAuthService auth,
    AppDbContext dbContext,
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
        if (account == null) return Redirect("/login");

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


        // kontrola odpovedi (pokud request byl zrusen, tak se nic neposle do db)
        if (accessToken == null || refreshToken == null) {
            return Redirect("/app/account?tab=settings");
        }


        // zapsani do db
        var token = await dbContext.AccountAccessTokens.FindAsync(
            [Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE, account.Id],
            cancellationToken: ct
        );

        if (token == null) {
            dbContext.AccountAccessTokens.Add(new Account.AccountAccessToken(
                userId: account.Id,
                platform: Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE,
                accessToken: accessToken,
                refreshToken: refreshToken,
                type: Account.AccountAccessToken.AccountAccessTokenType.BEARER
            ));
        } else {
            token.AccessToken = accessToken;
            token.RefreshToken = refreshToken;
            token.Type = Account.AccountAccessToken.AccountAccessTokenType.BEARER;
        }

        await dbContext.SaveChangesAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return Redirect("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account?tab=settings");
    }
}
