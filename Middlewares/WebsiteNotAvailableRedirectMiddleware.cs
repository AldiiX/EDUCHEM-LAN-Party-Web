namespace EduchemLPR.Middlewares;





public class WebsiteNotAvailableRedirectMiddleware(RequestDelegate next) {

    public async Task InvokeAsync(HttpContext context) {
        string path = context.Request.Path.Value ?? "/";

        if(
            path.StartsWith("/fonts/") ||
            path.StartsWith("/css/main.css")
            ) {
            await next(context);
            return;
        }

        if(path != "/" && Program.ENV.TryGetValue("WEBSITE_AVAILABLE", out var websiteAvailable) && websiteAvailable == "false") {
            context.Response.Redirect("/");
            return;
        }

        await next(context);
    }
}