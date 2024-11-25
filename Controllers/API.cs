using System.Diagnostics;
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

    [HttpPost("computers/reserve")]
    public IActionResult ReservePC([FromBody] Dictionary<string, object?> data) { // TODO: Optimalizovat rychlost
        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? pcID = data.TryGetValue("id", out var _pcid) ? _pcid?.ToString() : null;
        if (pcID == null) return BadRequest("Missing 'id' parameter");


        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand($@"
            START TRANSACTION;

            -- Odstranění rezervací z počítačů
            UPDATE computers
            SET reserved_by = NULL
            WHERE reserved_by = @userId;

            -- Odstranění uživatele z JSON seznamu rezervací v místnostech
            UPDATE rooms
            SET reserved_by = JSON_REMOVE(
                reserved_by,
                JSON_UNQUOTE(
                    JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR))
                )
            )
            WHERE JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)) IS NOT NULL;


            -- Přidání nové rezervace pro počítač
            UPDATE computers
            SET reserved_by = @userId
            WHERE id = @pcid{(acc.AccountType is not ("ADMIN" or "TEACHER") ? " AND reserved_by IS NULL" : "")};

            COMMIT;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@pcid", pcID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Computer not found");



        return Created();
    }

    [HttpDelete("computers/reserve")]
    public IActionResult DeletePCReservation([FromBody] Dictionary<string, object?> data) {
        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? pcID = data.TryGetValue("id", out var _pcid) ? _pcid?.ToString() : null;
        if (pcID == null) return BadRequest("Missing 'id' parameter");

        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand();
        cmd.Connection = conn;
        cmd.CommandText = @"
            UPDATE computers
            SET reserved_by = NULL
            WHERE id = @pcid
        ";
        if(acc.AccountType is not "ADMIN" and "TEACHER") cmd.CommandText += " AND reserved_by = @userId";

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@pcid", pcID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Computer not found or not reserved by you");

        return NoContent();
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
                    ["reservedByClass"] = null,
                };
            }

            else {
                var reservedBy = new JsonArray();
                foreach (var name in room.ReservedByName) reservedBy.Add(name);

                var reservedByClass = new JsonArray();
                foreach (var name in room.ReservedByClass) reservedByClass.Add(name);

                obj = new JsonObject {
                    ["id"] = room.ID,
                    ["limitOfSeats"] = room.LimitOfSeats,
                    ["reservedBy"] = reservedBy,
                    ["reservedByMe"] = room.ReservedBy.Contains(acc.ID),
                    ["reservedByClass"] = acc.AccountType is "TEACHER" or "ADMIN" ? reservedByClass : null,
                };
            }

            array.Add(obj);
        }

        return Ok(array);
    }

    [HttpPost("rooms/reserve")]
    public IActionResult ReserveRoom([FromBody] Dictionary<string, object?> data) {
        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? roomID = data.TryGetValue("id", out var _roomid) ? _roomid?.ToString() : null;
        if (roomID == null) return BadRequest("Missing 'id' parameter");

        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand(@"
            START TRANSACTION;

            -- Odstranění rezervací z počítačů
            UPDATE computers
            SET reserved_by = NULL
            WHERE reserved_by = @userId;

            -- Odstranění rezervací z místností
            UPDATE rooms
            SET reserved_by = JSON_REMOVE(
                reserved_by,
                JSON_UNQUOTE(
                    JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR))
                )
            )
            WHERE JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)) IS NOT NULL;

            -- Přidání nové rezervace pro místnost
            UPDATE rooms
            SET reserved_by = JSON_ARRAY_APPEND(
                reserved_by,
                '$',
                CAST(@userId AS CHAR)
            )
            WHERE id = @roomid;

            COMMIT;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@roomid", roomID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Room not found");

        return Created();
    }

    [HttpDelete("rooms/reserve")]
    public IActionResult DeleteRoomReservation([FromBody] Dictionary<string, object?> data) {
        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        // příprava proměnných
        string? roomID = data.TryGetValue("id", out var _roomid) ? _roomid?.ToString() : null;
        if (roomID == null) return BadRequest("Missing 'id' parameter");

        int userID = acc.ID;
        if (acc.AccountType is "ADMIN" or "TEACHER")
            userID = data.TryGetValue("userID", out var _userid) ? int.TryParse(_userid?.ToString(), out var parsed) ? parsed : acc.ID : acc.ID;



        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand(@"
            UPDATE rooms
            SET reserved_by = JSON_REMOVE(
                reserved_by,
                JSON_UNQUOTE(JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)))
            )
            WHERE id = @roomid AND JSON_SEARCH(reserved_by, 'one', CAST(@userId AS CHAR)) IS NOT NULL
        ", conn);

        cmd.Parameters.AddWithValue("@userId", userID);
        cmd.Parameters.AddWithValue("@roomid", roomID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Room not found or not reserved by you");

        return NoContent();
    }
}