using EduchemLP.Server.Classes;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduchemLP.Server.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireAuthAttribute : ActionFilterAttribute {
    
    public override void OnActionExecuting(ActionExecutingContext context) {
        var path = context.HttpContext.Request.Path.Value!;
        User? account = Auth.ReAuthUser();

        if (path.StartsWith("/api") && account == null) {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (account == null) {
            HttpContextService.Current.Session.SetString("AfterLoginRedirect", context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);

            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        context.HttpContext.Items["loggeduser"] = account;
        base.OnActionExecuting(context);
    }
}