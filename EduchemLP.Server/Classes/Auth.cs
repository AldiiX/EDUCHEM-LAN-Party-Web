using EduchemLP.Server.Services;
using Account = EduchemLP.Server.Classes.Objects.User;

namespace EduchemLP.Server.Classes;




public static class Auth {

    public static bool UserIsLoggedIn() {
        return HttpContextService.Current.Session.Get("loggeduser") != null;
    }

    public static Account? AuthUser(in string email, in string plainPassword, in bool updateUserByConnectedPlatforms = false) => Account.Auth(email, plainPassword, updateUserByConnectedPlatforms);

    public static async Task<Account?> ReAuthUserAsync() {
        if (!UserIsLoggedIn()) return null;

        var loggedUser = HttpContextService.Current.Session.GetObject<Account>("loggeduser");
        if (loggedUser == null) {
            HttpContextService.Current.Session.Remove("loggeduser");
            return null;
        }

        var acc = await Account.GetByIdAsync(loggedUser.ID);
        if (acc == null) {
            HttpContextService.Current.Session.Remove("loggeduser");
            return null;
        }

        // pokud se zmenilo heslo, zrusi se session
        if (acc.Password != loggedUser.Password) {
            HttpContextService.Current.Session.Remove("loggeduser");
            return null;
        }

        // jinak se obnovi data
        HttpContextService.Current.Session.SetObject("loggeduser", acc);
        HttpContextService.Current.Items["loggeduser"] = acc;
        return acc;
    }

    public static Account? ReAuthUser() => ReAuthUserAsync().Result;

    public static void Logout() {
        HttpContextService.Current.Session.Remove("loggeduser");
    }
}