using System.Text.Json;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Repositories;
using MySqlConnector;

namespace EduchemLP.Server.Services;





public class AuthService(IDatabaseService db, IHttpContextAccessor http, IAccountRepository accounts) : IAuthService {


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

    /*public async Task<Account?> RegisterAsync(string username, string email, string plainPassword, string? displayName, CancellationToken ct = default) {
        var hashed = Utilities.EncryptPassword(plainPassword);
        await using var conn = await db.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(@"
            insert into accounts (uuid, username, display_name, email, password, created_at)
            values (gen_random_uuid(), @u, @d, @e, @p, now());
            select * from accounts where username = @u and email = @e;
        ", conn);

        cmd.Parameters.AddWithValue("u", username);
        cmd.Parameters.AddWithValue("d", (object?)displayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("e", email);
        cmd.Parameters.AddWithValue("p", hashed);

        try {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            return new Account(
                reader.GetGuid("uuid"),
                reader.GetString("username"),
                reader.GetString("display_name"),
                reader.GetString("avatar_url"),
                reader.GetString("email"),
                reader.GetString("password"),
                reader.GetDateTime("created_at"),
                Enum.TryParse<Account.AccountPlan>(reader.GetString("plan"), out var plan) ? plan : Account.AccountPlan.FREE
            );
        } catch (NpgsqlException e) when (e.SqlState == "23505") {
            return null;
        }
    }
    */

    private async Task<Account?> GetAccountByIdentifierAsync(string identifier, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);

        const string sql = "select * from users where id = @id or email = @id";


        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", identifier);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new Account(reader);
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
        await using var conn = await db.GetOpenConnectionAsync(ct);
        const string sql = "update users set last_logged_in = now() where id = @id";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", account.Id);
        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }
}