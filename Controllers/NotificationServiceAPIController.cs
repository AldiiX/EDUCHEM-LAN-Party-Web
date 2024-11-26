using EduchemLPR.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;



[ApiController]
[Route("api/notifications")]
public class NotificationServiceAPIController(WebSocketService ws) : Controller {

    [HttpGet]
    public async Task Get(CancellationToken cancellationToken) {
        Response.ContentType = "text/event-stream";

        var clientId = Guid.NewGuid();
        await using var writer = new StreamWriter(Response.Body);
        ws.RegisterClient(writer);

        cancellationToken.Register(() => ws.UnregisterClient(clientId));

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

}