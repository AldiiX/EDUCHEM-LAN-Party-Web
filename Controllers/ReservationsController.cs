using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;





public class ReservationsController : Controller {

    [HttpGet("/reservations"), HttpGet("/rezervace")]
    public IActionResult Index() {
        return View("/Views/Reservations.cshtml");
    }
}