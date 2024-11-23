using System.Security.Cryptography;
using System.Text.Json;
using EduchemLPR.Classes.Objects;
using EduchemLPR.Services;

namespace EduchemLPR.Classes;





public static class Utilities {


    public static class Cookie {
        public static void Set(in string key, in string? value, in bool saveToTemp = true) {
            if (saveToTemp && HttpContextService.Current.Items["tempcookie_" + key] != null) return;

            HttpContextService.Current.Items["tempcookie_" + key] = value;
            HttpContextService.Current.Response.Cookies.Append(key, value ?? "null", new CookieOptions() {
                //HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
                Domain = Program.DEVELOPMENT_MODE ? "" : ".adminsphere.me",
                //Secure = !Program.DEVELOPMENT_MODE,
            });
        }

        public static string? Get(in string key) => HttpContextService.Current.Request.Cookies[key];

        public static bool Exists(in string key) => HttpContextService.Current.Request.Cookies.ContainsKey(key);

        public static void Delete(in string key) {
            HttpContextService.Current.Response.Cookies.Append(key, "", new CookieOptions() {
                //HttpOnly = true,
                IsEssential = true,
                Expires = DateTime.UtcNow.AddDays(-1),
                Domain = Program.DEVELOPMENT_MODE ? "" : ".adminsphere.me",
                //Secure = !Program.DEVELOPMENT_MODE,
            });
        }

        public static void Remove(in string key) => Delete(key);
    }



    public static void SetObject<T>(this ISession session, in string key, in T value) {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? GetObject<T>(this ISession session, in string key) where T : class? {
        var value = session.GetString(key);
        return value == null ? null : JsonSerializer.Deserialize<T>(value);
    }

    public static T? GetObject<T>(this ISession session, string key) where T : struct {
        var value = session.GetString(key);
        return value == null ? (T?)null : JsonSerializer.Deserialize<T>(value);
    }

    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    public static User GetLoggedAccountFromContext() {
        if(HttpContextService.Current.Items["loggeduser"] is not User account) throw new Exception("Account not found in context");
        return account;
    }

    public static User? GetLoggedAccountFromContextOrNull() {
        return HttpContextService.Current.Items["loggeduser"] is not User account ? null : account;
    }

    public static string GenerateKey() {
        var key = new byte[32];
        using var rng = RNGCryptoServiceProvider.Create();
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }
}