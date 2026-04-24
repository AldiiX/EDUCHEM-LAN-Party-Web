using System.Text.Json.Nodes;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Data;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLP.Server.Controllers;


[Route("/_be/instagram/oauth")]
public class InstagramOAuthController(
    IAuthService auth,
    AppDbContext dbContext,
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
        if (account == null) return Redirect("/login");


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


        // kontrola odpovedi (pokud request byl zrusen, tak se nic neposle do db)
        if (accessToken == null) {
            return Redirect("/app/account?tab=settings");
        }


        // zapsani do db
        var token = await dbContext.AccountAccessTokens.FindAsync(
            [Account.AccountAccessToken.AccountAccessTokenPlatform.INSTAGRAM, account.Id],
            cancellationToken: ct
        );

        if (token == null) {
            dbContext.AccountAccessTokens.Add(new Account.AccountAccessToken(
                userId: account.Id,
                platform: Account.AccountAccessToken.AccountAccessTokenPlatform.INSTAGRAM,
                accessToken: accessToken,
                refreshToken: null,
                type: Account.AccountAccessToken.AccountAccessTokenType.BEARER
            ));
        } else {
            token.AccessToken = accessToken;
            token.RefreshToken = null;
            token.Type = Account.AccountAccessToken.AccountAccessTokenType.BEARER;
        }

        await dbContext.SaveChangesAsync(ct);

        // znovu reauth
        account = await auth.ReAuthAsync(ct);
        if(account is null) return Redirect("/app/account?tab=settings");


        await accounts.UpdateAvatarByConnectedPlatformAsync(account, ct);
        return Redirect("/app/account");
    }
}
