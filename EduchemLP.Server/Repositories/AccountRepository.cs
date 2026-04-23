using System.Text;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using EduchemLP.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.Repositories;

public class AccountRepository(
    EduchemLpDbContext db,
    IHttpContextAccessor http
) : IAccountRepository {

    public async Task<Account?> GetByIdAsync(int id, CancellationToken ct = default) {
        return await db.Accounts
            .AsNoTracking()
            .Include(account => account.AccessTokens)
            .FirstOrDefaultAsync(account => account.Id == id, ct);
    }

    public async Task<List<Account>> GetAllAsync(CancellationToken ct = default) {
        return await db.Accounts
            .AsNoTracking()
            .Include(account => account.AccessTokens)
            .OrderBy(account => account.DisplayName)
            .ToListAsync(ct);
    }

    public async Task UpdateLastLoggedInAsync(Account account, CancellationToken ct = default) {
        var dbAccount = await db.Accounts.FirstOrDefaultAsync(x => x.Id == account.Id, ct);
        if (dbAccount == null) return;

        dbAccount.LastLoggedIn = DateTime.UtcNow;
        dbAccount.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAvatarByConnectedPlatformAsync(Account account, CancellationToken ct = default) {
        var dbAccount = await db.Accounts
            .Include(x => x.AccessTokens)
            .FirstOrDefaultAsync(x => x.Id == account.Id, ct);
        if (dbAccount == null) return;

        // novy avatar
        string? newAvatarLink = null;

        // zjisteni veci podle Discordu
        Account.AccountAccessToken? discordToken = dbAccount.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD);
        Account.AccountAccessToken? googleToken  = dbAccount.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE);
        Account.AccountAccessToken? githubToken  = dbAccount.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GITHUB);

        // google
        if (googleToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateGoogleAccessTokenAsync(dbAccount, ct);

            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            userInfoRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var userInfoResponse = await client.SendAsync(userInfoRequest, ct);
            var userInfo = JsonNode.Parse(await userInfoResponse.Content.ReadAsStringAsync(ct));

            newAvatarLink = userInfo?["picture"]?.ToString();
        }

        // github
        else if (githubToken != null) {
            var client = new HttpClient();
            var accessToken = githubToken.AccessToken;
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("User-Agent", "EDUCHEM LAN Party");

            var response = await client.GetAsync("https://api.github.com/user", ct);
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));

            newAvatarLink = content?["avatar_url"]?.ToString();
        }

        // discord
        else if (discordToken != null) {
            var client = new HttpClient();
            var accessToken = await GenerateDiscordAccessTokenAsync(dbAccount, ct);
            if (accessToken == null) return;

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://discord.com/api/users/@me", ct);
            var content = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct));

            // ziskani avataru
            var avatarId = content?["avatar"]?.ToString();
            var avatarHash = content?["discriminator"]?.ToString();
            var accountId = content?["id"]?.ToString();

            if (avatarId != null && avatarHash != null) {
                newAvatarLink = "https://cdn.discordapp.com/avatars/" + accountId + "/" + avatarId + ".png?size=256";
            }
        }

        dbAccount.Avatar = newAvatarLink ?? dbAccount.Avatar;
        dbAccount.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // aktualizace avataru v session
        var sessionAcc = JsonNode.Parse(http.HttpContext!.Session.GetString("loggedaccount") ?? "");
        if (sessionAcc == null) return;

        sessionAcc.AsObject()["avatar"] = newAvatarLink;
        http.HttpContext.Session.SetString("loggedaccount", sessionAcc.ToJsonString());
    }

    public async Task<Account?> CreateAsync(string email, string displayName, string? @class, Account.AccountGender gender, Account.AccountType accountType, bool sendToEmail = false, bool enableReservation = false, CancellationToken ct = default) {
        var password = Utilities.GenerateRandomPassword();
        var user = new Account(
            id: 0,
            displayName: displayName,
            email: email,
            password: Utilities.EncryptPassword(password),
            @class: @class,
            type: accountType,
            createdAt: DateTime.UtcNow,
            lastUpdated: DateTime.UtcNow,
            lastLoggedIn: null,
            gender: gender,
            avatar: null,
            banner: null,
            accessTokens: [],
            enableReservation: enableReservation
        );

        db.Accounts.Add(user);
        await db.SaveChangesAsync(ct);

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

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", discordAccessToken.AccessToken);

        // zjištění platnosti tokenu
        var testRequest = await client.GetAsync("https://discord.com/api/users/@me", ct);

        // když je token neplatný - přegeneruje se
        if (testRequest.StatusCode != System.Net.HttpStatusCode.Unauthorized) return testRequest.IsSuccessStatusCode ? discordAccessToken.AccessToken : null;

        var refreshClient = new HttpClient();
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");

        refreshRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["DISCORD_CLIENT_ID"] },
            { "client_secret", Program.ENV["DISCORD_CLIENT_SECRET"] },
            { "grant_type", "refresh_token" },
            { "refresh_token", discordAccessToken.RefreshToken },
            { "redirect_uri", Controllers.DiscordOAuthController.REDIRECT_URI }
        });

        var refreshResponse = await refreshClient.SendAsync(refreshRequest, ct);
        var refreshContent = JsonNode.Parse(await refreshResponse.Content.ReadAsStringAsync(ct));

        // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
        if (refreshContent?["access_token"] == null || refreshContent["refresh_token"] == null) {
            var token = await db.AccountAccessTokens
                .FirstOrDefaultAsync(x => x.UserId == account.Id && x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD, ct);
            if (token != null) {
                db.AccountAccessTokens.Remove(token);
                await db.SaveChangesAsync(ct);
            }

            return null;
        }

        // update v db
        var trackedToken = await db.AccountAccessTokens
            .FirstOrDefaultAsync(x => x.UserId == account.Id && x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.DISCORD, ct);
        if (trackedToken == null) return null;

        trackedToken.AccessToken = refreshContent["access_token"]?.ToString();
        trackedToken.RefreshToken = refreshContent["refresh_token"]?.ToString();
        await db.SaveChangesAsync(ct);

        return refreshContent["access_token"]?.ToString();

    }

    public async Task<string?> GenerateGoogleAccessTokenAsync(Account account, CancellationToken ct = default) {
        var googleAccessToken = account.AccessTokens.FirstOrDefault(x => x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE);
        if (googleAccessToken?.RefreshToken is null) return null;

        using var client = new HttpClient();

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

            // token nebyl úspěšně obnoven, pravdepodobne uzivatel zrusil pristup, odstrani se to i z db
            if (refreshData?["access_token"] == null) {
                var token = await db.AccountAccessTokens
                    .FirstOrDefaultAsync(x => x.UserId == account.Id && x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE, ct);
                if (token != null) {
                    db.AccountAccessTokens.Remove(token);
                    await db.SaveChangesAsync(ct);
                }

                return null;
            }

            string newAccessToken = refreshData["access_token"]!.ToString();
            var trackedToken = await db.AccountAccessTokens
                .FirstOrDefaultAsync(x => x.UserId == account.Id && x.Platform == Account.AccountAccessToken.AccountAccessTokenPlatform.GOOGLE, ct);
            if (trackedToken == null) return null;

            trackedToken.AccessToken = newAccessToken;
            await db.SaveChangesAsync(ct);

            return newAccessToken;
        }

        if (testResponse.IsSuccessStatusCode)
            return googleAccessToken.AccessToken;

        return null;
    }
}
