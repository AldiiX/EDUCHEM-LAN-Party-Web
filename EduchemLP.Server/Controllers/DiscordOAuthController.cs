using EduchemLP.Server.Classes;
using Microsoft.AspNetCore.Mvc;

namespace EduchemLP.Server.Controllers;


[Route("/_be/discord/oauth")]
public class DiscordOAuthController : Controller {

    #if DEBUG
        private const string REDIRECT_URI = "http://localhost:3154/_be/discord/oauth";
    #else
        private const string REDIRECT_URI = "https://educhemlan.emsio.cz/_be/discord/oauth";
    #endif


    [HttpGet]
    public IActionResult Index([FromQuery] string code) {
        Console.WriteLine(code);

        // ziskani tokenu z codu
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
        request.Headers.Add("Accept", "application/json");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
            { "client_id", Program.ENV["DISCORD_CLIENT_ID"] },
            { "client_secret", Program.ENV["DISCORD_CLIENT_SECRET"] },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", REDIRECT_URI }
        });

        using var response = client.SendAsync(request).Result;
        var body = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(body);


        return RedirectPermanent("/app/account");
    }
}