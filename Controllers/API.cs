using System.Text.Json;
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
                ["reservedByMe"] = false,
                ["reservedByClass"] = null,
            };

            else obj = new JsonObject {
                ["id"] = computer.ID,
                ["reservedBy"] = computer.ReservedByName,
                ["reservedByMe"] = computer.ReservedByName == null ? false : computer.ReservedBy == acc.ID,
                ["reservedByClass"] = acc.AccountType is "TEACHER" or "ADMIN" ? computer.ReservedByClass : null,
            };

            array.Add(obj);
        }

        return Ok(array);
    }

    [HttpGet("rooms")]
    public IActionResult GetAllRooms() {
        var accTask = Auth.ReAuthUserAsync();
        var roomsTask = Room.GetAllAsync();

        var array = new JsonArray();
        foreach (var room in roomsTask.Result) {
            JsonObject obj;
            var acc = accTask.Result;

            if (acc == null) {
                var reservedBy = new JsonArray();
                foreach (var _ in room.ReservedBy) reservedBy.Add("someone");

                obj = new JsonObject {
                    ["id"] = room.ID,
                    ["limitOfSeats"] = room.LimitOfSeats,
                    ["reservedBy"] = reservedBy,
                    ["reservedByMe"] = false,
                };
            }

            else {
                var reservedBy = new JsonArray();
                foreach (var name in room.ReservedByName) reservedBy.Add(name);

                obj = new JsonObject {
                    ["id"] = room.ID,
                    ["limitOfSeats"] = room.LimitOfSeats,
                    ["reservedBy"] = reservedBy,
                    ["reservedByMe"] = room.ReservedBy.Contains(acc.ID),
                };
            }

            array.Add(obj);
        }

        return Ok(array);
    }
}