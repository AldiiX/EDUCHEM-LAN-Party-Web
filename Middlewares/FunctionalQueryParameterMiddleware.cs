using EduchemLPR.Classes;

namespace EduchemLPR.Middlewares;

public class FunctionalQueryParameterMiddleware(RequestDelegate next) {

    public async Task InvokeAsync(HttpContext context) {
        var query = context.Request.Query.ToDictionary();

        if (query.TryGetValue("lg", out var _loginKey) && !string.IsNullOrEmpty(_loginKey)) {
            var loginKey = Utilities.DecryptStringWithKey(_loginKey.ToString(), "Tak vážení, přátelé, prosimvás!");
            var path = context.Request.Path.Value ?? "/";

            Auth.AuthUser(loginKey);
            context.Response.Redirect(path);
        }

        await next(context);
    }
}