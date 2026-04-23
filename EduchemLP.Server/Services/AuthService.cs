using System.Text.Json;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.Services;

public class AuthService(EduchemLpDbContext db, IHttpContextAccessor http, IAccountRepository accounts) : IAuthService {

    public async Task<Account?> LoginAsync(string identifier, string plainPassword, CancellationToken ct = default) {
        var acc = await GetAccountByIdentifierAsync(identifier, ct);
        if (acc == null) return null;

        if (!Utilities.VerifyPassword(plainPassword, acc.Password)) return null;
        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(acc, JsonSerializerOptions.Web));
        http.HttpContext.Items["loggedaccount"] = acc;
        _ = UpdateLastLoggedInAsync(acc, ct);
        return acc;
    }

    public async Task<Account?> ForceLoginAsync(string identifier, CancellationToken ct = default) {
        var acc = await GetAccountByIdentifierAsync(identifier, ct);
        if (acc == null) return null;

        http.HttpContext!.Session.Clear();
        await http.HttpContext.Session.CommitAsync(ct);
        http.HttpContext.Session.SetString("loggedaccount", JsonSerializer.Serialize(acc, JsonSerializerOptions.Web));
        http.HttpContext.Items["loggedaccount"] = acc;
        await http.HttpContext.Session.CommitAsync(ct);
        return acc;
    }

    public async Task<Account?> ReAuthAsync(CancellationToken ct = default) {
        var json = http.HttpContext?.Session.GetString("loggedaccount");
        if (string.IsNullOrEmpty(json)) return null;

        var sessionAcc = JsonSerializer.Deserialize<Account>(json, JsonSerializerOptions.Web);
        if (sessionAcc == null) return null;

        var acc = await accounts.GetByIdAsync(sessionAcc.Id, ct);
        if (acc == null || acc.Password != sessionAcc.Password) return null;

        _ = UpdateLastLoggedInAsync(acc, ct);
        http.HttpContext!.Items["loggedaccount"] = acc;
        http.HttpContext!.Session.SetString("loggedaccount", JsonSerializer.Serialize(acc, JsonSerializerOptions.Web));
        return acc;
    }

    private async Task<Account?> GetAccountByIdentifierAsync(string identifier, CancellationToken ct) {
        if (identifier.Contains('@')) {
            return await db.Accounts
                .AsNoTracking()
                .Include(x => x.AccessTokens)
                .FirstOrDefaultAsync(x => x.Email == identifier, ct);
        }

        if (!int.TryParse(identifier, out var id)) {
            return null;
        }

        return await db.Accounts
            .AsNoTracking()
            .Include(x => x.AccessTokens)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Account?> ReAuthFromContextOrNullAsync(CancellationToken ct = default) {
        if (http.HttpContext == null) return null;
        if (!http.HttpContext.Items.ContainsKey("loggedaccount")) return await ReAuthAsync(ct);

        var str = http.HttpContext?.Session.GetString("loggedaccount");
        if (string.IsNullOrEmpty(str)) return null;

        var acc = JsonSerializer.Deserialize<Account>(str, JsonSerializerOptions.Web);
        if (acc != null) return acc;

        return await ReAuthAsync(ct);
    }

    public async Task<bool> UpdateLastLoggedInAsync(Account account, CancellationToken ct = default) {
        var dbAccount = await db.Accounts.FirstOrDefaultAsync(x => x.Id == account.Id, ct);
        if (dbAccount == null) return false;

        dbAccount.LastLoggedIn = DateTime.UtcNow;
        dbAccount.LastUpdated = DateTime.UtcNow;
        return await db.SaveChangesAsync(ct) > 0;
    }
}
