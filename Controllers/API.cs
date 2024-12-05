using System.Text;
using System.Text.Json.Nodes;
using EduchemLPR.Classes;
using EduchemLPR.Classes.Objects;
using EduchemLPR.Models;
using EduchemLPR.Services;
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
                ["isTeacherPC"] = computer.IsTeacherPC,
            };

            else obj = new JsonObject {
                ["id"] = computer.ID,
                ["reservedBy"] = computer.ReservedByName,
                ["reservedByMe"] = computer.ReservedByName == null ? false : computer.ReservedBy == acc.ID,
                ["reservedByClass"] = acc.AccountType is "TEACHER" or "ADMIN" ? computer.ReservedByClass : null,
                ["isTeacherPC"] = computer.IsTeacherPC,
            };

            array.Add(obj);
        }

        return Ok(array);
    }

    [HttpPost("computers/reserve")]
    public IActionResult ReservePC([FromBody] Dictionary<string, object?> data, [FromServices] SSEService ws) {
        var reservationsEnabledTask = Utilities.AreReservationsEnabledAsync();

        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? pcID = data.TryGetValue("id", out var _pcid) ? _pcid?.ToString() : null;
        if (pcID == null) return BadRequest("Missing 'id' parameter");


        // kontrola, zda je rezervace povolena
        if(reservationsEnabledTask.Result == false) return new JsonResult(new { success = false, message = "Reservations are disabled" }) { StatusCode = 403 };


        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand($@"
            INSERT INTO reservations (computer_id, user_id)
            VALUES (@pcid, @userId)
            ON DUPLICATE KEY UPDATE computer_id = @pcid, user_id = @userId, created_at = NOW(), room_id = NULL;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@pcid", pcID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Computer not found");



        // poslání notifikace clientům
        _ = ws.NotifyClientsAsync(new { clientAction = "refresh", datetime = DateTime.Now });



        return Created();
    }

    [HttpDelete("computers/reserve")]
    public IActionResult DeletePCReservation([FromBody] Dictionary<string, object?> data, [FromServices] SSEService ws) {
        var reservationsEnabledTask = Utilities.AreReservationsEnabledAsync();

        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        string? pcID = data.TryGetValue("id", out var _pcid) ? _pcid?.ToString() : null;
        if (pcID == null) return BadRequest("Missing 'id' parameter");



        // kontrola, zda je rezervace povolena
        if(reservationsEnabledTask.Result == false) return new JsonResult(new { success = false, message = "Reservations are disabled" }) { StatusCode = 403 };



        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand();
        cmd.Connection = conn;
        cmd.CommandText = @"
            DELETE FROM reservations
            WHERE user_id = @userId
        ";

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@pcid", pcID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Computer not found or not reserved by you");



        // poslání notifikace clientům
        _ = ws.NotifyClientsAsync(new { clientAction = "refresh", datetime = DateTime.Now });



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
    public IActionResult ReserveRoom([FromBody] Dictionary<string, object?> data, [FromServices] SSEService ws) {
        var reservationsEnabledTask = Utilities.AreReservationsEnabledAsync();
        var accTask = Auth.ReAuthUserAsync();

        var acc = accTask.Result;
        if (acc == null) return Unauthorized("Not logged in");

        string? roomID = data.TryGetValue("id", out var _roomid) ? _roomid?.ToString() : null;
        if (roomID == null) return BadRequest("Missing 'id' parameter");


        // kontrola, zda je rezervace povolena
        if(reservationsEnabledTask.Result == false) return new JsonResult(new { success = false, message = "Reservations are disabled" }) { StatusCode = 403 };


        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand(@"
            INSERT INTO reservations (room_id, user_id)
            VALUES (@roomid, @userId)
            ON DUPLICATE KEY UPDATE room_id = @roomid, user_id = @userId, created_at = NOW(), computer_id = NULL;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", acc.ID);
        cmd.Parameters.AddWithValue("@roomid", roomID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Room not found");



        // poslání notifikace clientům
        _ = ws.NotifyClientsAsync(new { clientAction = "refresh", datetime = DateTime.Now });



        return Created();
    }

    [HttpDelete("rooms/reserve")]
    public IActionResult DeleteRoomReservation([FromBody] Dictionary<string, object?> data, [FromServices] SSEService ws) {
        var reservationsEnabledTask = Utilities.AreReservationsEnabledAsync();

        var acc = Auth.ReAuthUser();
        if (acc == null) return Unauthorized("Not logged in");

        // příprava proměnných
        string? roomID = data.TryGetValue("id", out var _roomid) ? _roomid?.ToString() : null;
        if (roomID == null) return BadRequest("Missing 'id' parameter");

        int userID = acc.ID;
        if (acc.AccountType is "ADMIN" or "TEACHER")
            userID = data.TryGetValue("userID", out var _userid) ? int.TryParse(_userid?.ToString(), out var parsed) ? parsed : acc.ID : acc.ID;


        // kontrola, zda je rezervace povolena
        if(reservationsEnabledTask.Result == false) return new JsonResult(new { success = false, message = "Reservations are disabled" }) { StatusCode = 403 };


        using var conn = Database.GetConnection();
        if(conn == null) return StatusCode(502, "Database connection error");

        using var cmd = new MySqlCommand(@"
            DELETE FROM reservations
            WHERE user_id = @userId;
        ", conn);

        cmd.Parameters.AddWithValue("@userId", userID);
        cmd.Parameters.AddWithValue("@roomid", roomID);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound("Room not found or not reserved by you");



        // poslání notifikace clientům
        _ = ws.NotifyClientsAsync(new { clientAction = "refresh", datetime = DateTime.Now });


        
        return NoContent();
    }

    [HttpGet("appsettings")]
    public IActionResult GetAppSettings() {
        var enableReservationsTask = Database.GetDataAsync("enableReservations");

        var obj = new JsonObject {
            ["enableReservations"] = bool.TryParse(enableReservationsTask.Result?.ToString(), out var _parsed) && _parsed,
        };

        return new JsonResult(obj);
    }

    [HttpGet("users")]
    public IActionResult GetUsers() {
        var acc = Auth.ReAuthUser();
        if (acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Not logged in" });

        if (acc.AccountType is not "ADMIN") return new UnauthorizedObjectResult(new { success = false, message = "Not an admin" });

        var users = Classes.Objects.User.GetAll();
        // var array = new JsonArray();

        return new JsonResult(users);
    }

    [HttpPost("users/resetkey")]
    public IActionResult ResetUserKey([FromBody] Dictionary<string, object?> data) {
        var acc = Auth.ReAuthUser();
        if (acc == null) return new UnauthorizedObjectResult(new { success = false, message = "Not logged in" });

        if (acc.AccountType is not "ADMIN") return new UnauthorizedObjectResult(new { success = false, message = "Not an admin" });

        string? userID = data.TryGetValue("id", out var _userid) ? _userid?.ToString() : null;
        bool sendToEmail = data.TryGetValue("sendToEmail", out var _sendToEmail) ? bool.TryParse(_sendToEmail?.ToString(), out var _parsed) ? _parsed : true : true;
        if (userID == null) return new BadRequestObjectResult(new { success = false, message = "Missing 'id' parameter" });

        using var conn = Database.GetConnection();
        if(conn == null) return new StatusCodeResult(502);

        using var cmd = new MySqlCommand(@"
            START TRANSACTION;            

            UPDATE users
            SET auth_key = @newKey
            WHERE id = @userId;

            SELECT * FROM users WHERE id = @userId;

            COMMIT;
        ", conn);

        var newKey = Utilities.GenerateRandomAuthKey();
        cmd.Parameters.AddWithValue("@newKey", newKey);
        cmd.Parameters.AddWithValue("@userId", userID);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return new NotFoundObjectResult(new { success = false, message = "User not found" });




        // poslání emailu
        string? email = reader.GetObjectOrNull("email") as string ?? null;
        newKey = reader.GetObjectOrNull("auth_key") as string ?? newKey;

        string fallbackBody =
            $"""
             
             Ahoj, díky, že se účastníš LAN Party.
             Pokud nemáš vlastní setup, rezervuj si počítač, na kterém po celou dobu budeš.
             Pokud si bereš svůj vlastní setup, rezervuj místnost, kde svůj setup budeš mít. Nezapomeň si s sebou vzít i příslušenství včetně monitorů a prodlužováku.
             Rezervuj si to co nejdříve, protože kapacita je omezená.
     
     
             Tvůj autentizační klíč: {newKey}
             Odkaz na stránku: https://{Program.ROOT_DOMAIN}/rezervace?lg={Convert.ToBase64String(Encoding.UTF8.GetBytes(newKey))}

             """;

        if(email != null && sendToEmail) _ = EmailService.SendHTMLEmailAsync(email, "Registrace do Educhem LAN Party", "~/Views/Emails/UserRegistered.cshtml", new EmailUserRegisterModel(newKey, $"https://{Program.ROOT_DOMAIN}/rezervace?lg={Convert.ToBase64String(Encoding.UTF8.GetBytes(newKey))}"), HttpContext.RequestServices, fallbackBody);
        //if(email != null && sendToEmail) _ = EmailService.SendPlainTextEmailAsync(email, "EDUCHEM LAN Party: Registrace", fallbackBody);


        return new OkObjectResult(new { success = true, message = "Key reset", emailMessage = fallbackBody });
    }
}