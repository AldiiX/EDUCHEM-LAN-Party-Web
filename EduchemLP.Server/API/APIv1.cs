using System.Data;
using System.Globalization;
using System.Text.Json.Nodes;
using EduchemLP.Server.Classes;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.API;


[ApiController]
[Route("api/v1")]
public class APIv1 : Controller {


    [HttpGet]
    public IActionResult Test() {
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
            ["accountType"] = acc?.AccountType,
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


        return acc == null ? new UnauthorizedObjectResult(new { success = false, message = "Neplatný email nebo heslo" }) : new JsonResult(new { success = true, account = acc});
    }

    [HttpDelete("loggeduser")]
    public IActionResult Logout() {
        HttpContextService.Current.Items["loggeduser"] = null;
        HttpContextService.Current.Session.Remove("loggeduser");
        Response.Cookies.Delete("educhemlpr_session");
        return new NoContentResult();
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
        if(acc is not { AccountType: "ADMIN" }) return new UnauthorizedObjectResult(new { success = false, message = "Nejsi přihlášený jako admin" });

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
}