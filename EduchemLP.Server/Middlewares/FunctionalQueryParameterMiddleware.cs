using System.Text;
using EduchemLP.Server.Classes;

namespace EduchemLP.Server.Middlewares;

public class FunctionalQueryParameterMiddleware(RequestDelegate next) {

    public async Task InvokeAsync(HttpContext context) {
        var query = context.Request.Query.ToDictionary();

        // TODO: dodělat login pomocí odkazu
        /*if (query.TryGetValue("lg", out var _loginKey) && !string.IsNullOrEmpty(_loginKey)) {
            var loginKeyBytes = Convert.FromBase64String(_loginKey.ToString());
            string loginKey = Encoding.UTF8.GetString(loginKeyBytes);

            var path = context.Request.Path.Value ?? "/";

            Auth.AuthUser(email, passw);
            context.Response.Redirect(path);
        }*/

        skip:
        await next(context);
    }
}