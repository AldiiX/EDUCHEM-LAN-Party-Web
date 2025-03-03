using EduchemLP.Server.Classes;

namespace EduchemLP.Server.Middlewares;



/*
 * Tento middleware kontroluje např. přihlášení 
 */
public class BeforeInitMiddleware(RequestDelegate next){
    public async Task InvokeAsync(HttpContext context) {
        string path = context.Request.Path.Value ?? "/";





        // async věci
        var accTask = Auth.ReAuthUserAsync();




        // zbytek
        await accTask;
        await next(context);
    }
}