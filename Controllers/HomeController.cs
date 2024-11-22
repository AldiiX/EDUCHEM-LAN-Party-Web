using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;

public class HomeController : Controller {


    [Route("/")]
    public IActionResult Index() {
        return View("/Views/Index.cshtml");
    }

    [Route("/info")]
    public IActionResult Info() {
        return RedirectPermanent("/educhem_xmas_lan_2024.pdf");
    }
}