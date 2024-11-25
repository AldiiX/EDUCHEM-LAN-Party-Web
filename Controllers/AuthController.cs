using EduchemLPR.Classes;
using EduchemLPR.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;

public class AuthController : Controller {

    [HttpGet("/login")]
    public IActionResult Login([FromQuery] string? r) {
        return View("/Views/Login.cshtml");
    }

    [HttpGet("/auth/logout")]
    public IActionResult Logout([FromQuery] string? r) {
        HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        HttpContext.Response.Headers["Pragma"] = "no-cache";
        HttpContext.Response.Headers["Expires"] = "-1";

        HttpContext.Session.Remove("loggeduser");
        HttpContext.Items.Remove("loggeduser");

        return Redirect(r ?? "/");
    }

    [HttpPost("/login")]
    public IActionResult Auth([FromForm] string? key, [FromQuery] string? r) {
        if (key == null) {
            TempData["error"] = "Zadej prosím klíč.";
            return RedirectToAction("Login");
        }

        if(key.StartsWith("_")) {
            TempData["error"] = "Účet s tímto klíčem je nedostupný.";
            return RedirectToAction("Login");
        }

        var account = EduchemLPR.Classes.Objects.User.Auth(key);
        if (account == null) {
            TempData["error"] = "Účet s tímto klíčem neexistuje.";
            return RedirectToAction("Login");
        }

        HttpContextService.Current.Session.SetObject("loggeduser", account);
        HttpContextService.Current.Items["loggeduser"] = account;
        return RedirectPermanent(r ?? "/");
    }
}