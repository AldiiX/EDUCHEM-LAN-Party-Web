using EduchemLPR.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLPR.Controllers;



[ApiController]
[Route("api/sse/main")]
public class MainSSEAPIController(SSEService sse) : Controller {

    [HttpGet]
    public async Task Get(CancellationToken cancellationToken) {
        Response.ContentType = "text/event-stream";

        var clientId = Guid.NewGuid();
        await using var writer = new StreamWriter(Response.Body);
        sse.RegisterClient(writer);

        cancellationToken.Register(() => sse.UnregisterClient(clientId));

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

}