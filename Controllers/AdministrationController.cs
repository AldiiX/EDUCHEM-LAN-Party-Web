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
        string email = data["email"].ToString();
        string cls = data["class"].ToString();
        string authKey = Utilities.GenerateRandomKey();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(cls)) return false;

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
        _ = EmailService.SendHTMLEmailAsync(email, "Registrace do Educhem LAN Party", "~/Views/Emails/UserRegistered.cshtml", new EmailUserRegisterModel(authKey, "https://lanparty.educhem.it/rezervace?lg=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(authKey))), HttpContext.RequestServices);

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