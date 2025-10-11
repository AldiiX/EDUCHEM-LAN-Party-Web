using EduchemLP.Server.Services;

namespace EduchemLP.Server.Middlewares;



/*
 * Tento middleware kontroluje např. přihlášení 
 */
public class BeforeInitMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory) {

    public async Task InvokeAsync(HttpContext context) {
        var ct = context.RequestAborted;

        string path = context.Request.Path.Value ?? "/";
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();





        // async věci
        var accTask = auth.ReAuthAsync(ct);




        // zbytek
        await accTask;
        await next(context);
    }
}