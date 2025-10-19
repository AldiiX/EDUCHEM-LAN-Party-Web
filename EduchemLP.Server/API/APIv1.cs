using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Models;
using EduchemLP.Server.Repositories;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace EduchemLP.Server.API;


[ApiController]
[Route("api/v1")]
public class APIv1(
    IDatabaseService db,
    IAuthService auth,
    IDbLoggerService dbLogger,
    IAppSettingsService appSettings,
    IAccountRepository accounts,
    IWebSocketHub webSocketHub
) : Controller {


    [HttpGet]
    public IActionResult Index() {
        return new JsonResult(new { success = true, message = "API v1" });
    }

    #if DEBUG
    [HttpGet("gpw")]
    public IActionResult GeneratePasswordEncryption([FromQuery] string password) {
        return new JsonResult(new { password, encrypted = Utilities.EncryptPassword(password) });
    }
    #endif


    [HttpGet("loggeduser"), HttpGet("me")]
    public async Task<IActionResult> GetLoggedUser(CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" });

        // vraceni json objektu
        return new JsonResult(acc.ToPublicJsonNode());
    }

    [HttpPut("loggeduser"), HttpPut("me")]
    public async Task<IActionResult> EditLoggedUser([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" });

        // overeni parametru
        Account.AccountGender? gender = data.TryGetValue("gender", out var _g) ? Enum.TryParse(_g?.ToString(), out Account.AccountGender _g2) ? _g2 : null : null;
        string? avatar = data.TryGetValue("avatar", out var _avatar) ? _avatar?.ToString() : null;
        string? banner = data.TryGetValue("banner", out var _banner) ? _banner?.ToString() : null;

        // poslani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            UPDATE users 
            SET 
                avatar=IF(@avatar IS NULL, NULL, avatar), -- povleno pouze smazani
                banner=IF(@banner IS NULL, NULL, banner), -- povleno pouze smazani
                gender=@gender
            WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@avatar", avatar);
        command.Parameters.AddWithValue("@banner", banner);
        command.Parameters.AddWithValue("@gender", gender.ToString()?.ToUpper());
        command.Parameters.AddWithValue("@id", acc.Id);

        if(await command.ExecuteNonQueryAsync(ct) <= 0) return new JsonResult(new { success = false, message = "Nepodařilo se změnit údaje." }) { StatusCode = 500 };

        // zapsani do logu
        await dbLogger.LogInfoAsync($"Uživatel {acc.DisplayName} ({acc.Email}) si změnil údaje.", "user-edit", ct);

        // nastaveni aktualni instance do session
        await auth.ReAuthAsync(ct);
        return new NoContentResult();
    }

    [HttpPost("loggeduser/password"), HttpPost("me/password")]
    public async Task<IActionResult> ChangeLoggedUserPassword([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if (acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" });

        string? oldPassword = data.TryGetValue("oldPassword", out var _oldPassword) ? _oldPassword?.ToString() : null;
        string? newPassword = data.TryGetValue("newPassword", out var _newPassword) ? _newPassword?.ToString() : null;
        if (oldPassword == null || newPassword == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'oldPassword' nebo 'newPassword'" });

        // stare hesla se musi shodovat
        if (!Utilities.VerifyPassword(oldPassword, acc.Password)) return new BadRequestObjectResult(new { success = false, message = "Staré heslo je špatně" });

        // overeni platnosti hesla
        switch (newPassword.Length) { // overeni lengthu hesla
            case < 8: return new BadRequestObjectResult(new { success = false, message = "Heslo musí mít alespoň 8 znaků" });
            case > 64: return new BadRequestObjectResult(new { success = false, message = "Heslo musí mít maximálně 64 znaků" });
        }

        if (newPassword == oldPassword) return new BadRequestObjectResult(new { success = false, message = "Nové heslo se nesmí shodovat se starým heslem" });
        if(!Utilities.IsPasswordValid(newPassword)) return new BadRequestObjectResult(new { success = false, message = "Heslo musí obsahovat alespoň jedno velké písmeno, jedno číslo a jeden speciální znak." });

        // encrypnuti hesla
        var encryptedNewPassword = Utilities.HashPassword(newPassword);

        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            UPDATE users SET password=@password WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@password", encryptedNewPassword);
        command.Parameters.AddWithValue("@id", acc.Id);

        if(await command.ExecuteNonQueryAsync(ct) <= 0) return new JsonResult(new { success = false, message = "Nepodařilo se změnit heslo." }) { StatusCode = 500 };

        // zapsani do logu
        await dbLogger.LogInfoAsync($"Uživatel {acc.DisplayName} ({acc.Email}) si změnil heslo.", "password-change", ct);

        // nastaveni aktualni instance do session
        await auth.ReAuthAsync(ct);
        return new NoContentResult();
    }

    [HttpGet("loggeduser/connections"), HttpGet("me/connections")]
    public async Task<IActionResult> GetLoggedUserConnections(CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if (acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" });


        var arr = new JsonArray();

        foreach (var token in acc.AccessTokens) {
            arr.Add(token.Platform.ToString().ToUpper());
        }

        return new JsonResult(arr);
    }

    [HttpDelete("loggeduser/connections"), HttpDelete("me/connections")]
    public async Task<IActionResult> DeleteLoggedUserConnection([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if (acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" });

        // overeni parametru
        string? p = data.TryGetValue("platform", out var _p) ? _p?.ToString()?.ToUpper() : null;
        if (p == null || !Enum.TryParse(p.ToUpper(), out Account.AccountAccessToken.AccountAccessTokenPlatform platform)) return new BadRequestObjectResult(new { success = false, message = "Neplatná platforma" });



        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if (conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            DELETE FROM users_access_tokens WHERE platform=@platform AND user_id=@userId;
            """, conn
        );

        command.Parameters.AddWithValue("@platform", platform.ToString().ToUpper());
        command.Parameters.AddWithValue("@userId", acc.Id);


        return await command.ExecuteNonQueryAsync(ct) > 0 ? new NoContentResult() : new JsonResult(new { success = false, message = "Nepodařilo se odstranit připojení." }) { StatusCode = 500 };
    }

    [HttpPost("loggeduser"), HttpPost("me")]
    public async Task<IActionResult> LoginUser([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        string? email = data.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? password = data.TryGetValue("password", out var _password) ? _password?.ToString() : null;
        if(email == null || password == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'email' nebo 'password'" });

        // pokud email neobsahuje @educhem.cz, doplni se
        if(!email.Contains('@') && !email.Contains("@educhem.cz")) {
            email = email.Trim() + "@educhem.cz";
        }

        var acc = await auth.LoginAsync(email, password, ct);
        if(acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Neplatný email nebo heslo" });

        return new JsonResult(new { success = true, account = acc.ToPublicJsonNode() });
    }

    [HttpDelete("loggeduser"), HttpDelete("me")]
    public IActionResult Logout() {
        HttpContextService.Current.Items["loggedaccount"] = null;
        HttpContextService.Current.Session.Remove("loggedaccount");
        Response.Cookies.Delete("educhemlp_session");
        return new NoContentResult();
    }

    [HttpGet("lg")]
    public async Task<IActionResult> LoginAndRedirect([FromQuery] string u, [FromQuery] string? redirect, CancellationToken ct = default) {
        string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(u)).Split(" ");
        string email = credentials[0];
        string password = credentials.Length > 1 ? credentials[1] : "";

        await auth.LoginAsync(email, password, ct);
        return Redirect(redirect ?? "/app");
    }

    [HttpGet("appsettings")]
    public async Task<IActionResult> GetAppSettings(CancellationToken ct = default) {
        return new OkObjectResult(new {
                reservationsStatus = (await appSettings.GetReservationsStatusAsync(ct)).ToString().ToUpper(),
                reservationsEnabledFrom = await appSettings.GetReservationsEnabledFromAsync(ct),
                reservationsEnabledTo = await appSettings.GetReservationsEnabledToAsync(ct),
                reservationsEnabledRightNow = await appSettings.AreReservationsEnabledRightNowAsync(ct),
                chatEnabled = await appSettings.GetChatEnabledAsync(ct),
            }
        );
    }

    [HttpPut("appsettings")]
    public async Task<IActionResult> SetAppSettings([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null || acc.Type < Account.AccountType.ADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nelze upravit nastavení, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        IAppSettingsService.ReservationStatusType? status = data.TryGetValue("reservationsStatus", out var _status) ? Enum.TryParse(_status?.ToString(), out IAppSettingsService.ReservationStatusType _status2) ? _status2 : null : null;
        DateTime? from = data.TryGetValue("reservationsEnabledFrom", out var _from) ? DateTime.TryParse(_from?.ToString(), out var _from2) ? _from2 : null : null;
        DateTime? to = data.TryGetValue("reservationsEnabledTo", out var _to) ? DateTime.TryParse(_to?.ToString(), out var _to2) ? _to2 : null : null;
        bool? chatEnabled = data.TryGetValue("chatEnabled", out var _chatEnabled) ? bool.TryParse(_chatEnabled?.ToString(), out var _chatEnabled2) ? _chatEnabled2 : null : null;

        // datetime musi byt v UTC

        // asynch picovinky
        var t1 = Task.Run(() => {
            if(status == null) return;
            appSettings.SetReservationsStatusAsync((IAppSettingsService.ReservationStatusType) status, ct);
        }, ct);

        var t2 = Task.Run(() => {
            if(from == null) return;
            appSettings.SetReservationsEnabledFromAsync((DateTime) from, ct);
        }, ct);

        var t3 = Task.Run(() => {
            if(to == null) return;
            appSettings.SetReservationsEnabledToAsync((DateTime) to, ct);
        }, ct);

        var t4 = Task.Run(() => {
            if(chatEnabled == null) return;
            appSettings.SetChatEnabledAsync((bool) chatEnabled, ct);
        }, ct);


        // oznameni do sync socketu
        var json = new { action = "updateAppSettings"}.ToJsonString();
        await webSocketHub.BroadcastAsync("sync", json, ct);


        Task.WaitAll(t1, t2, t3, t4);
        return new NoContentResult();
    }







    [HttpGet("adm/users")]
    public async Task<IActionResult> GetUsers(CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc?.Type < Account.AccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        var users = await accounts.GetAllAsync(ct);
        var array = new JsonArray();

        foreach (var user in users.Where(user => user.Id >= 0)) {
            array.Add(user.ToPublicJsonNode());
        }

        return new JsonResult(array);
    }

    [HttpPost("adm/users")]
    public async Task<IActionResult> AddUser([FromBody] Dictionary<string, object?> body, CancellationToken ct = default) {
        var loggedUser = await auth.ReAuthFromContextOrNullAsync(ct);
        if(loggedUser == null || loggedUser.Type < Account.AccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        string? email = body.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? displayName = body.TryGetValue("displayName", out var _displayName) ? _displayName?.ToString() : null;
        string? @class = body.TryGetValue("class", out var _class) ? _class?.ToString() : null;
        string? accountType = body.TryGetValue("type", out var _accountType) ? _accountType?.ToString() : null;
        string? gender = body.TryGetValue("gender", out var _gender) ? _gender?.ToString() : null;
        bool sendToEmail = body.TryGetValue("sendToEmail", out var _sendToEmail) && bool.TryParse(_sendToEmail?.ToString(), out var _sendToEmail2) && _sendToEmail2;

        if(email == null || displayName == null || accountType == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'email', 'displayName' nebo 'type'" });
        var accountTypeParsed = Enum.TryParse(accountType, out Account.AccountType _ac) ? _ac : Account.AccountType.STUDENT;

        if(loggedUser.Type < accountTypeParsed && loggedUser.Type != Account.AccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš vytvořit uživatele s vyššími právy." });

        var genderParsed = Enum.TryParse(gender, out Account.AccountGender _g) ? _g : Account.AccountGender.OTHER;
        var account = await accounts.CreateAsync(email, displayName, @class, genderParsed, accountTypeParsed, sendToEmail, ct);
        if(account == null) return new JsonResult(new { success = false, message = "Chyba při vytváření uživatele." }) { StatusCode = 500};

        // zapassani do logu
        await dbLogger.LogInfoAsync($"Uživatel {account.DisplayName} ({account.Email}) byl vytvořen uživatelem {loggedUser.DisplayName} ({loggedUser.Email}).", "user-create", ct);

        return new JsonResult(new { success = true, message = "Uživatel byl vytvořen." });
    }

    [HttpDelete("adm/users")]
    public async Task<IActionResult> DeleteUser([FromBody] Dictionary<string, object?> body, CancellationToken ct = default) {
        // auth sendera
        var loggedUser = await auth.ReAuthFromContextOrNullAsync(ct);
        if(loggedUser is null) return new UnauthorizedObjectResult(new { success = false, message = "Tuto akci může provést jen přihlášený uživatel." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = body.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var targetUser = await accounts.GetByIdAsync((int) id, ct);
        if(targetUser == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen" });

        // overeni prav
        if(loggedUser.Type <= targetUser.Type && loggedUser.Type != Account.AccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš smazat uživatele s vyššími nebo stejnými právy." });



        // query
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return new StatusCodeResult(500);
        await using var command = new MySqlCommand(
            """
            DELETE FROM users WHERE id=@id;
            """, conn
        );
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(ct);

        // zapsani do logu
        await dbLogger.LogInfoAsync($"Uživatel {targetUser.DisplayName} ({targetUser.Email}) byl smazán uživatelem {loggedUser.DisplayName} ({loggedUser.Email}).", "user-delete", ct);

        return new JsonResult(new { success = true, message = "Uživatel byl smazán." });
    }

    [HttpPost("adm/users/passwordreset")]
    public async Task<IActionResult> ResetUserPassword([FromBody] Dictionary<string, object?> data, CancellationToken ct = default) {
        // zjisteni prihlasenyho uzivatele + jeho perms
        var loggedAccount = await auth.ReAuthFromContextOrNullAsync(ct);
        if(loggedAccount == null || loggedAccount.Type < Account.AccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = data.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var user = await accounts.GetByIdAsync((int) id, ct);
        if(user == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen." });

        // overeni prav obou uzivatelu
        if(loggedAccount.Type <= user.Type && loggedAccount.Type != Account.AccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš obnovit heslo uživateli s vyššími nebo stejnými právy." });

        // vytvoreni noveho hesla
        var newPassword = Utilities.GenerateRandomPassword();
        var encryptedPassword = Utilities.EncryptPassword(newPassword);

        // db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return new StatusCodeResult(500);

        await using var command = new MySqlCommand(
            """
            UPDATE users 
            SET 
                password = @password,
                last_updated = NOW()
            WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@password", encryptedPassword);
        command.Parameters.AddWithValue("@id", user.Id);
        await command.ExecuteNonQueryAsync(ct);

        // odeslani emailu
        string credentialsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email + " " + newPassword));
        string webLink = "https://" + Program.ROOT_DOMAIN + "/api/v1/lg?u=" + Uri.EscapeDataString(credentialsBase64);
        _ = EmailService.SendHTMLEmailAsync(user.Email, "Obnovení hesla k EDUCHEM LAN Party", "~/Views/Emails/UserResetPassword.cshtml",
            new EmailUserRegisterModel(newPassword, webLink, user.Email)
        );

        // zapsani do logu
        await dbLogger.LogInfoAsync($"Heslo uživatele {user.DisplayName} ({user.Email}) bylo resetováno uživatelem {loggedAccount.DisplayName} ({loggedAccount.Email}).", "password-reset", ct);

        return new JsonResult(new { success = true, message = "Heslo bylo obnoveno a odesláno na email." });
    }

    [HttpPut("adm/users")]
    public async Task<IActionResult> EditUser([FromBody] Dictionary<string, object?> body, CancellationToken ct = default) {
        // zjisteni prihlasenyho uzivatele + jeho perms
        var loggedAccount = await auth.ReAuthFromContextOrNullAsync(ct);
        if(loggedAccount == null || loggedAccount.Type < Account.AccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze upravit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = body.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var user = await accounts.GetByIdAsync((int) id, ct);
        if(user == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen." });

        // overeni prav obou uzivatelu
        if(loggedAccount.Type <= user.Type && loggedAccount.Type != Account.AccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš upravit uživatele s vyššími nebo stejnými právy." });

        // overeni parametru
        string? email = body.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? displayName = body.TryGetValue("displayName", out var _displayName) ? _displayName?.ToString() : null;
        string? @class = body.TryGetValue("class", out var _class) ? _class?.ToString() : null;
        Account.AccountType? accountType = body.TryGetValue("type", out var _accountType) ? Enum.TryParse(_accountType?.ToString(), out Account.AccountType _ac) ? _ac : null : null;
        Account.AccountGender? gender = body.TryGetValue("gender", out var _gender) ? Enum.TryParse(_gender?.ToString(), out Account.AccountGender _g) ? _g : null : null;
        bool? enableReservation = body.TryGetValue("enableReservation", out var _enableReservation) ? bool.TryParse(_enableReservation?.ToString(), out var _er) ? _er : null : null;
        email = email == "" ? null : email?.Trim();
        displayName = displayName == "" ? null : displayName?.Trim();
        @class = @class == "" ? null : @class?.Trim();


        // pokud accounttype neni parsovany
        if(accountType == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'type'" });

        // pokud je gender neco mimo enum
        if(gender == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'gender'" });

        // zjisteni zda loggeduser nedava uzivateli vyssi accountType
        if(loggedAccount.Type <= accountType && loggedAccount.Type != Account.AccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš dát uživateli vyšší roli než máš ty." });



        // zapsani do db
        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return new StatusCodeResult(500);

        await using var command = new MySqlCommand(
            """
            UPDATE `users` 
            SET 
                `email`=IF(@email IS NULL, email, @email),
                `display_name`=IF(@displayName IS NULL, display_name, @displayName),
                `class`=@class,
                `account_type`=IF(@accountType IS NULL, account_type, @accountType),
                `gender`=IF(@gender IS NULL, gender, @gender),
                `enable_reservation`=IF(@enableReservation IS NULL, enable_reservation, @enableReservation),
                `last_updated`=NOW()
            WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@displayName", displayName);
        command.Parameters.AddWithValue("@class", @class);
        command.Parameters.AddWithValue("@accountType", accountType.ToString()?.ToUpper());
        command.Parameters.AddWithValue("@gender", gender.ToString()?.ToUpper());
        command.Parameters.AddWithValue("@enableReservation", enableReservation);


        // zapsani do logu
        await dbLogger.LogInfoAsync($"Uživatel {user.DisplayName} ({user.Email}) byl upraven uživatelem {loggedAccount.DisplayName} ({loggedAccount.Email}).", "user-edit", ct);

        var r = await command.ExecuteNonQueryAsync(ct);
        return r > 0 ? new JsonResult(new { success = true, message = "Uživatel byl upraven." }) : new JsonResult(new { success = false, message = "Uživatel nebyl upraven." }) { StatusCode = 400 };
    }

    [HttpGet("adm/logs")]
    public async Task<IActionResult> GetLogs(CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null || acc.Type < Account.AccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit logy, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        await using var conn = await db.GetOpenConnectionAsync(ct);
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            SELECT * FROM logs ORDER BY `date` DESC;
            """, conn
        );

        await using var reader = await command.ExecuteReaderAsync(ct);

        var array = new JsonArray();
        while(await reader.ReadAsync(ct)) {
            var obj = new JsonObject {
                ["id"] = reader.GetInt32("id"),
                ["type"] = (Enum.TryParse(reader.GetString("exact_type"), out IDbLoggerService.LogType _type) ? _type : IDbLoggerService.LogType.INFO).ToString(),
                ["exactType"] = reader.GetString("exact_type"),
                ["message"] = reader.GetString("message"),
                ["date"] = reader.GetDateTime("date"),
            };

            array.Add(obj);
        }

        return new JsonResult(array);
    }

    [HttpPost("adm/users/loginas")]
    public async Task<IActionResult> LoginAsUser([FromBody] JsonNode body, CancellationToken ct = default) {
        var acc = await auth.ReAuthFromContextOrNullAsync(ct);
        if(acc == null || acc.Type < Account.AccountType.ADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        int? userId = int.TryParse(body["uid"]?.GetValue<string>(), out var _id) ? _id : null;
        if(userId == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'uid'" });

        var targetUser = await accounts.GetByIdAsync(userId.Value, ct);
        if(targetUser == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen." });

        // pokud má target user vyšší nebo stejné práva jako přihlašující se user, nepovoli se to krom superadmina
        if(targetUser.Type >= acc.Type && acc.Type != Account.AccountType.SUPERADMIN)
            return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš se přihlásit jako uživatel s vyššími nebo stejnými právy." });

        var success = await auth.ForceLoginAsync(targetUser.Email, ct) != null;
        if(!success) return new ObjectResult(new { success = false, message = "Nepodařilo se přihlásit jako tento uživatel." }) { StatusCode = 500 };

        return new JsonResult(new { success = true, message = "Přihlášení proběhlo úspěšně." });
    }
}