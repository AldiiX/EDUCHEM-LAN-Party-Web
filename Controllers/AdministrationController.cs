using EduchemLPR.Attributes;
using EduchemLPR.Classes;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;

public class AdministrationController : Controller {

    [RequireAuth, HttpGet("/administration"), HttpGet("/admin")]
    public IActionResult Index() {
        var acc = Utilities.GetLoggedAccountFromContextOrNull();
        if (acc is not { AccountType: "ADMIN" }) return new UnauthorizedResult();

        return View("/Views/Administration.cshtml");
    }
}