using System.Text.Json.Nodes;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Data;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLP.Server.Controllers;


[Route("/_be/discord/oauth")]
public class DiscordOAuthController(IAuthService auth, EduchemLpDbContext db, IAccountRepository accounts) : Controller {

    #if DEBUG
        public const string REDIRECT_URI = "http://localhost:3154/_be/discord/oauth";
    #else
        public const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/discord/oauth";
    #endif


    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string code, CancellationToken ct = default) {
        var account = await auth.ReAuthFromContextOrNullAsync(ct);
        if (account == null) return Redirect("/login");



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

        using var response = await client.SendAsync(request, ct);
        var body = JsonNode.Parse(response.Content.ReadAsStringAsync(ct).Result);



        // kontrola odpovedi (pokud request byl zrusen, tak se nic neposle do db)
        if (body?["access_token"] == null || body["refresh_token"] == null) {
            return Redirect("/app/account?tab=settings");
        }



        // zapsani do db
        var token = await db.AccountAccessTokens.FindAsync(
            [Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD, account.Id],
            cancellationToken: ct
        );

        if (token == null) {
            db.AccountAccessTokens.Add(new Account.AccountAccessToken(
                userId: account.Id,
                platform: Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD,
                accessToken: body?["access_token"]?.ToString(),
                refreshToken: body?["refresh_token"]?.ToString(),
                type: Account.AccountAccessToken.AccountAccessTokenType.BEARER
            ));
        } else {
            token.AccessToken = body?["access_token"]?.ToString();
            token.RefreshToken = body?["refresh_token"]?.ToString();
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
