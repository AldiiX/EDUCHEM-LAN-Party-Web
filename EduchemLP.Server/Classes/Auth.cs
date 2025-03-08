using EduchemLP.Server.Services;
using Account = EduchemLP.Server.Classes.Objects.User;

namespace EduchemLP.Server.Classes;




public static class Auth {

    public static bool UserIsLoggedIn() {
        return HttpContextService.Current.Session.Get("loggeduser") != null;
    }

    public static Account? AuthUser(in string email, in string hashedPassword) => Account.Auth(email, hashedPassword);

    public static async Task<Account?> ReAuthUserAsync() {
        if (!UserIsLoggedIn()) return null;

        var loggedUser = HttpContextService.Current.Session.GetObject<Account>("loggeduser");
        if (loggedUser == null) {
            //Console.WriteLine("Logged user is null");
            HttpContextService.Current.Session.Remove("loggeduser");
            return null;
        }

        var acc = await Account.AuthAsync(loggedUser.Email, loggedUser.Password);
        if (acc == null) {
            //Console.WriteLine("Acc is null");
            HttpContextService.Current.Session.Remove("loggeduser");
            return null;
        }

        HttpContextService.Current.Session.SetObject("loggeduser", acc);
        HttpContextService.Current.Items["loggeduser"] = acc;
        return acc;
    }

    public static Account? ReAuthUser() => ReAuthUserAsync().Result;

    public static void Logout() {
        HttpContextService.Current.Session.Remove("loggeduser");
    }
}