using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;

public class HomeController : Controller {


    [Route("/")]
    public IActionResult Index() {
        if(Program.ENV.TryGetValue("WEBSITE_AVAILABLE", out var websiteAvailable) && websiteAvailable == "false") {
            return View("/Views/WebsiteNotAvailable.cshtml");
        }

        return View("/Views/Index.cshtml");
    }

    [Route("/info")]
    public IActionResult Info() {
        return RedirectPermanent("/educhem_xmas_lan_2024.pdf");
    }

    [Route("/unsubscribe")]
    public IActionResult Unsubscribe() => Redirect("/");
}