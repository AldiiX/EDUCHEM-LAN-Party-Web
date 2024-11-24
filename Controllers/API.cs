using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLPR.Classes;
using EduchemLPR.Classes.Objects;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace EduchemLPR.Controllers;


[ApiController]
[Route("api")]
public class API : Controller {

    private async Task<bool> DeleteAllReservations1Async(User acc) {
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;

        await using var cmd = new MySqlCommand("UPDATE computers SET reserved_by = NULL WHERE reserved_by = @id", conn);
        cmd.Parameters.AddWithValue("@id", acc.ID);
        await cmd.ExecuteNonQueryAsync();

        return true;
    }

    private async Task<bool> DeleteAllReservations2Async(User acc) { // TODO: Opravit (nefunguje)
        await using var conn = await Database.GetConnectionAsync();
        if (conn == null) return false;

        await using var cmd = new MySqlCommand(@"
            UPDATE rooms
            SET reserved_by = JSON_REMOVE(
                reserved_by,
                JSON_UNQUOTE(JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)))
            ) 
            WHERE JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)) IS NOT NULL;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        await cmd.ExecuteNonQueryAsync();

        return true;
    }



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

    [HttpPost("computers/reserve")]
    public IActionResult ReservePC([FromBody] Dictionary<string, object?> data) { // TODO: Optimalizovat rychlost
        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? pcID = data.TryGetValue("id", out var _pcid) ? _pcid?.ToString() : null;
        if (pcID == null) return BadRequest("Missing 'id' parameter");



        // odstranění aktuálních rezervací, pokud existují
        var cmd1Task = DeleteAllReservations1Async(acc);
        var cmd2Task = DeleteAllReservations2Async(acc);

        if(cmd1Task.Result == false || cmd2Task.Result == false) return StatusCode(502, "Chyba při odstranění starých rezervací.");



        // rezervace nového počítače
        using var conn = Database.GetConnection();
        if (conn == null) return StatusCode(502, "Chyba při připojení k databázi");

        using var cmd3 = new MySqlCommand("UPDATE computers SET reserved_by = @id WHERE id = @pcid", conn);
        cmd3.Parameters.AddWithValue("@pcid", pcID);
        cmd3.Parameters.AddWithValue("@id", acc.ID);
        cmd3.ExecuteNonQuery();



        return Created();
    }
}