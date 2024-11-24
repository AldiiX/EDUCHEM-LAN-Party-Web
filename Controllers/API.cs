using System.Text.Json.Nodes;
using EduchemLPR.Classes;
using EduchemLPR.Classes.Objects;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;


[ApiController]
[Route("api")]
public class API : Controller {

    [HttpGet("computers")]
    public IActionResult GetAllComputers() {
        var accTask = Auth.ReAuthUserAsync();
        var computersTask = Computer.GetAllAsync();
        var array = new JsonArray();

        foreach (var computer in computersTask.Result) {
            JsonObject obj;
            var acc = accTask.Result;

            if (acc == null) obj = new JsonObject {
                ["id"] = computer.ID,
                ["reservedBy"] = computer.ReservedByName == null ? null : "someone",
                ["reservedByMe"] = false
            };

            else obj = new JsonObject {
                ["id"] = computer.ID,
                ["reservedBy"] = computer.ReservedByName,
                ["reservedByMe"] = computer.ReservedByName == null ? false : computer.ReservedBy == acc.ID
            };

            array.Add(obj);
        }

        return Ok(array);
    }

    [HttpGet("rooms")]
    public IActionResult GetAllRooms() => Ok(Room.GetAll());
}