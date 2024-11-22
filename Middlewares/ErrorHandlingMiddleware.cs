using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
namespace EduchemLPR.Middlewares;





public class ErrorHandlingMiddleware(RequestDelegate next) {

    public async Task InvokeAsync(HttpContext context) {
        await next(context);

        // Pokud není nalezena žádná odpovídající cesta
        if (context.Response is { HasStarted: false, StatusCode: not (200 or 300 or 301 or 302 or 303 or 304) }) {

            var errorReasonPhrase = ReasonPhrases.GetReasonPhrase(context.Response.StatusCode);
            var path = context.Request.Path.Value ?? "";

            // pokud je to api error
            if (path.StartsWith("/api") || path.StartsWith("/iapi")) {
                var errorResponse = new {
                    success = false,
                    code = context.Response.StatusCode,
                    message = errorReasonPhrase
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                return;
            }



            context.Response.Redirect("/");
        }
    }
}