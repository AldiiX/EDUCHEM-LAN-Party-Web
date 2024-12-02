using System.Text;
using EduchemLPR.Attributes;
using EduchemLPR.Classes;
using EduchemLPR.Models;
using EduchemLPR.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;





public class AdministrationController : Controller {


    public bool CreateUser(IFormCollection data) {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if (acc is not { AccountType: "ADMIN" }) return false;

        string name = data["name"].ToString();
        string? email = data.TryGetValue("email", out var _emailVal) ? _emailVal.ToString() : null;
        string cls = data["class"].ToString();
        string authKey = Utilities.GenerateRandomAuthKey();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(cls)) return false;

        // vytvoreni obj do databaze
        using var conn = Database.GetConnection();
        if(conn == null) return false;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO `users` (`display_name`, `email`, `class`, `auth_key`) VALUES (@name, @email, @class, @auth_key)";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@class", cls);
        cmd.Parameters.AddWithValue("@auth_key", authKey);
        cmd.ExecuteNonQuery();

        // odeslani emailu
        string fallbackBody =
            $"""

             Ahoj, díky, že se účastníš LAN Party.
             Pokud nemáš vlastní setup, rezervuj si počítač, na kterém po celou dobu budeš.
             Pokud si bereš svůj vlastní setup, rezervuj místnost, kde svůj setup budeš mít. Nezapomeň si s sebou vzít i příslušenství včetně monitorů a prodlužováku.
             Rezervuj si to co nejdříve, protože kapacita je omezená.


             Tvůj autentizační klíč: {authKey}
             Odkaz na stránku: https://{Program.ROOT_DOMAIN}/rezervace?lg={Convert.ToBase64String(Encoding.UTF8.GetBytes(authKey))}

             """;

        if(email != null) _ = EmailService.SendHTMLEmailAsync(email, "Registrace do Educhem LAN Party", "~/Views/Emails/UserRegistered.cshtml", new EmailUserRegisterModel(authKey, $"https://{Program.ROOT_DOMAIN}/rezervace?lg={Convert.ToBase64String(Encoding.UTF8.GetBytes(authKey))}"), HttpContext.RequestServices, fallbackBody);

        return true;
    }






    [RequireAuth, HttpPost("/administration")]
    public IActionResult HandleForms(IFormCollection form) {
        string? formName = form["formName"];

        switch (formName) {
            case "createUser": CreateUser(form); break;
        }

        return RedirectToAction("Index");
    }

    [RequireAuth, HttpGet("/administration")]
    public IActionResult Index() {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if (acc is not { AccountType: "ADMIN" }) return new UnauthorizedResult();

        return View("/Views/Administration.cshtml");
    }

    [HttpGet("/admin")]
    public IActionResult RedirectToAdmin() {
        return Redirect("/administration");
    }
}