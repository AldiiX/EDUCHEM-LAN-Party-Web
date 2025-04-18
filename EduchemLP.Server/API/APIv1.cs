﻿using System.Data;
using System.Text;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Models;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using User = EduchemLP.Server.Classes.Objects.User;

namespace EduchemLP.Server.API;


[ApiController]
[Route("api/v1")]
public class APIv1 : Controller {


    [HttpGet]
    public IActionResult Index() {
        return new JsonResult(new { success = true, message = "API v1" });
    }

    [HttpGet("loggeduser")]
    public IActionResult GetLoggedUser() {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        var obj = new JsonObject {
            ["id"] = acc?.ID,
            ["displayName"] = acc?.DisplayName,
            ["email"] = acc?.Email,
            ["class"] = acc?.Class,
            ["accountType"] = acc?.AccountType.ToString().ToUpper(),
            ["lastUpdated"] = acc?.LastUpdated,
            ["avatar"] = acc?.Avatar,
        };

        return acc == null ? new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený" }) : new JsonResult(obj);
    }

    [HttpPost("loggeduser")]
    public IActionResult LoginUser([FromBody] Dictionary<string, object?> data) {
        string? email = data.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? password = data.TryGetValue("password", out var _password) ? _password?.ToString() : null;
        if(email == null || password == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'email' nebo 'password'" });

        var acc = Auth.AuthUser(email, Utilities.EncryptPassword(password));
        if(acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Neplatný email nebo heslo" });

        var obj = new JsonObject {
            ["id"] = acc.ID,
            ["displayName"] = acc.DisplayName,
            ["email"] = acc.Email,
            ["class"] = acc.Class,
            ["accountType"] = acc.AccountType.ToString().ToUpper(),
            ["lastUpdated"] = acc.LastUpdated,
            ["avatar"] = acc.Avatar,
        };


        return new JsonResult(new { success = true, account = obj});
    }

    [HttpDelete("loggeduser")]
    public IActionResult Logout() {
        HttpContextService.Current.Items["loggeduser"] = null;
        HttpContextService.Current.Session.Remove("loggeduser");
        Response.Cookies.Delete("educhemlp_session");
        return new NoContentResult();
    }

    [HttpGet("lg")]
    public IActionResult LoginAndRedirect([FromQuery] string u, [FromQuery] string? redirect) {
        string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(u)).Split(" ");
        string email = credentials[0];
        string password = credentials.Length > 1 ? credentials[1] : "";

        _ = Auth.AuthUser(email, Utilities.EncryptPassword(password));

        return Redirect(redirect ?? "/app");
    }

    #if DEBUG
    [HttpGet("gpw")]
    public IActionResult GeneratePasswordEncryption([FromQuery] string password) {
        return new JsonResult(new { password, encrypted = Utilities.EncryptPassword(password) });
    }
    #endif





    [HttpGet("adm/users")]
    public IActionResult GetUsers() {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if(acc?.AccountType < Classes.Objects.User.UserAccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        using var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            SELECT * FROM users WHERE id > 0 ORDER BY display_name;
            """, conn
        );

        using var reader = command.ExecuteReader();
        if(reader == null) return new StatusCodeResult(500);

        var array = new JsonArray();
        while(reader.Read()) {
            var obj = new JsonObject {
                ["id"] = reader.GetInt32("id"),
                ["email"] = reader.GetString("email"),
                ["name"] = reader.GetString("display_name"),
                ["class"] = reader.GetStringOrNull("class"),
                ["gender"] = reader.GetStringOrNull("gender"),
                ["accountType"] = reader.GetString("account_type"),
                ["lastUpdated"] = reader.GetDateTime("last_updated"),
                ["lastLoggedIn"] = reader.GetObjectOrNull("last_logged_in") != null ? (DateTime)reader.GetValue("last_logged_in") : null,
                ["avatar"] = reader.GetStringOrNull("avatar"),
            };

            array.Add(obj);
        }

        return new JsonResult(array);
    }

    [HttpPost("adm/users")]
    public IActionResult AddUser([FromBody] Dictionary<string, object?> body) {
        var loggedUser = Utilities.GetLoggedAccountFromContextOrNull();
        if(loggedUser == null || loggedUser.AccountType < Classes.Objects.User.UserAccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        string? email = body.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? displayName = body.TryGetValue("displayName", out var _displayName) ? _displayName?.ToString() : null;
        string? @class = body.TryGetValue("class", out var _class) ? _class?.ToString() : null;
        string? accountType = body.TryGetValue("accountType", out var _accountType) ? _accountType?.ToString() : null;
        string? gender = body.TryGetValue("gender", out var _gender) ? _gender?.ToString() : null;
        bool sendToEmail = body.TryGetValue("sendToEmail", out var _sendToEmail) && bool.TryParse(_sendToEmail?.ToString(), out var _sendToEmail2) && _sendToEmail2;

        if(email == null || displayName == null || accountType == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'email', 'displayName' nebo 'accountType'" });
        var accountTypeParsed = Enum.TryParse(accountType, out User.UserAccountType _ac) ? _ac : Classes.Objects.User.UserAccountType.STUDENT;

        if(loggedUser.AccountType < accountTypeParsed && loggedUser.AccountType != Classes.Objects.User.UserAccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš vytvořit uživatele s vyššími právy." });

        var genderParsed = Enum.TryParse(gender, out User.UserGender _g) ? _g : Classes.Objects.User.UserGender.OTHER;
        var user = Classes.Objects.User.Create(email, displayName, @class, genderParsed, accountTypeParsed, sendToEmail);
        if(user == null) return new JsonResult(new { success = false, message = "Chyba při vytváření uživatele." }) { StatusCode = 500};

        // zapassani do logu
        DbLogger.Log(DbLogger.LogType.INFO, $"Uživatel {user.DisplayName} ({user.Email}) byl vytvořen uživatelem {loggedUser.DisplayName} ({loggedUser.Email}).", "user-create");

        return new NoContentResult();
    }

    [HttpDelete("adm/users")]
    public IActionResult DeleteUser([FromBody] Dictionary<string, object?> body) {
        // auth sendera
        var loggedUser = Utilities.GetLoggedAccountFromContextOrNull();
        if(loggedUser is null) return new UnauthorizedObjectResult(new { success = false, message = "Tuto akci může provést jen přihlášený uživatel." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = body.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var targetUser = Classes.Objects.User.GetById((int) id);
        if(targetUser == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen" });

        // overeni prav
        if(loggedUser.AccountType <= targetUser.AccountType && loggedUser.AccountType != Classes.Objects.User.UserAccountType.SUPERADMIN) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš smazat uživatele s vyššími nebo stejnými právy." });



        // query
        using var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(500);
        var command = new MySqlCommand(
            """
            DELETE FROM users WHERE id=@id;
            """, conn
        );
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();

        // zapsani do logu
        DbLogger.Log(DbLogger.LogType.INFO, $"Uživatel {targetUser.DisplayName} ({targetUser.Email}) byl smazán uživatelem {loggedUser.DisplayName} ({loggedUser.Email}).", "user-delete");

        return new NoContentResult();
    }

    [HttpPost("adm/users/passwordreset")]
    public IActionResult ResetUserPassword([FromBody] Dictionary<string, object?> data) {
        // zjisteni prihlasenyho uzivatele + jeho perms
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if(acc == null || acc.AccountType < Classes.Objects.User.UserAccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = data.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var user = Classes.Objects.User.GetById((int) id);
        if(user == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen." });

        // overeni prav obou uzivatelu
        if(acc.AccountType <= user.AccountType) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš obnovit heslo uživateli s vyššími nebo stejnými právy." });

        // vytvoreni noveho hesla
        var newPassword = Utilities.GenerateRandomPassword();
        var encryptedPassword = Utilities.EncryptPassword(newPassword);

        // db
        using var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            UPDATE users 
            SET 
                password = @password,
                last_updated = NOW()
            WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@password", encryptedPassword);
        command.Parameters.AddWithValue("@id", user.ID);
        command.ExecuteNonQuery();

        // odeslani emailu
        string webLink = "https://" + Program.ROOT_DOMAIN + "/api/v1/lg?u=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(user.Email + " " + newPassword));
        _ = EmailService.SendHTMLEmailAsync(user.Email, "Obnovení hesla k EDUCHEM LAN Party", "~/Views/Emails/UserResetPassword.cshtml",
            new EmailUserRegisterModel(newPassword, webLink, user.Email)
        );

        // zapsani do logu
        DbLogger.Log(DbLogger.LogType.INFO, $"Heslo uživatele {user.DisplayName} ({user.Email}) bylo resetováno uživatelem {acc.DisplayName} ({acc.Email}).", "password-reset");

        return new JsonResult(new { success = true, message = "Heslo bylo obnoveno a odesláno na email." });
    }

    [HttpPut("adm/users")]
    public IActionResult EditUser([FromBody] Dictionary<string, object?> body) {
        // zjisteni prihlasenyho uzivatele + jeho perms
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if(acc == null || acc.AccountType < Classes.Objects.User.UserAccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit uživatele, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        // zjisteni id uzivatele kteryho chceme mazat
        int? id = body.TryGetValue("id", out var _id) ? int.TryParse(_id?.ToString(), out var _id2) ? _id2 : null : null;
        if(id == null) return new BadRequestObjectResult(new { success = false, message = "Chybí parametr 'id'" });

        // zjisteni user instance uzivatele kteryho chceme mazat
        var user = Classes.Objects.User.GetById((int) id);
        if(user == null) return new NotFoundObjectResult(new { success = false, message = "Uživatel nenalezen." });

        // overeni prav obou uzivatelu
        if(acc.AccountType <= user.AccountType) return new UnauthorizedObjectResult(new { success = false, message = "Nemůžeš obnovit heslo uživateli s vyššími nebo stejnými právy." });

        // overeni parametru
        string? email = body.TryGetValue("email", out var _email) ? _email?.ToString() : null;
        string? displayName = body.TryGetValue("displayName", out var _displayName) ? _displayName?.ToString() : null;
        string? @class = body.TryGetValue("class", out var _class) ? _class?.ToString() : null;
        var accountType = body.TryGetValue("accountType", out var _accountType) ? Enum.TryParse(_accountType?.ToString(), out Classes.Objects.User.UserAccountType _ac) ? _ac : Classes.Objects.User.UserAccountType.STUDENT : Classes.Objects.User.UserAccountType.STUDENT;
        var gender = body.TryGetValue("accountType", out var _gender) ? Enum.TryParse(_gender?.ToString(), out User.UserGender _g) ? _g : Classes.Objects.User.UserGender.OTHER : Classes.Objects.User.UserGender.OTHER;
        email = email == "" ? null : email?.Trim();
        displayName = displayName == "" ? null : displayName?.Trim();
        @class = @class == "" ? null : @class?.Trim();



        // zapsani do db
        var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            UPDATE `users` 
            SET 
                `email`=IF(@email IS NULL, email, @email),
                `display_name`=IF(@displayName IS NULL, display_name, @displayName),
                `class`=@class,
                `account_type`=IF(@accountType IS NULL, account_type, @accountType),
                `gender`=IF(@gender IS NULL, gender, @gender),
                `last_updated`=NOW()
            WHERE id=@id;
            """, conn
        );

        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@displayName", displayName);
        command.Parameters.AddWithValue("@class", @class);
        command.Parameters.AddWithValue("@accountType", accountType.ToString().ToUpper());
        command.Parameters.AddWithValue("@gender", gender.ToString().ToUpper());


        // zapsani do logu
        DbLogger.LogInfo($"Uživatel {user.DisplayName} ({user.Email}) byl upraven uživatelem {acc.DisplayName} ({acc.Email}).", "user-edit");
        
        var r = command.ExecuteNonQuery();
        return r > 0 ? new JsonResult(new { success = true, message = "Uživatel byl upraven." }) : new JsonResult(new { success = false, message = "Uživatel nebyl upraven." }) { StatusCode = 400 };
    }

    [HttpGet("adm/logs")]
    public IActionResult GetLogs() {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if(acc == null || acc.AccountType < Classes.Objects.User.UserAccountType.TEACHER) return new UnauthorizedObjectResult(new { success = false, message = "Nelze zobrazit logy, pokud nejsi přihlášený, nebo nemáš dostatečná práva." });

        using var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(500);

        var command = new MySqlCommand(
            """
            SELECT * FROM logs ORDER BY `date` DESC;
            """, conn
        );

        using var reader = command.ExecuteReader();
        if(reader == null) return new StatusCodeResult(500);

        var array = new JsonArray();
        while(reader.Read()) {
            var obj = new JsonObject {
                ["id"] = reader.GetInt32("id"),
                ["type"] = (Enum.TryParse(reader.GetString("exact_type"), out DbLogger.LogType _type) ? _type : DbLogger.LogType.INFO).ToString(),
                ["exactType"] = reader.GetString("exact_type"),
                ["message"] = reader.GetString("message"),
                ["date"] = reader.GetDateTime("date"),
            };

            array.Add(obj);
        }

        return new JsonResult(array);
    }
}