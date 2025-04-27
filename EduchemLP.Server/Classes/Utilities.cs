using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Services;
using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes;





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

    public static T? GetValueOrNull<T>(this MySqlDataReader reader, string key) where T : struct {
        int ordinal = reader.GetOrdinal(key);
        if(reader.IsDBNull(ordinal)) return null;

        object value = reader.GetValue(ordinal);
        if(value is T result) return result;

        try {
            // Convert the value to the target type T
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception) {
            return null;
        }
    }

    public static string? GetStringOrNull(this MySqlDataReader reader, string key) {
        try {
            int ordinal = reader.GetOrdinal(key);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        } catch (Exception) {
            return null;
        }
    }

    public static User? GetLoggedAccountFromContextOrNull() {
        return HttpContextService.Current.Items["loggeduser"] as User;
    }

    public static string GenerateRandomPassword(int length = 24) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ěščřž!@*";
        var random = new Random();
        var passwordBuilder = new StringBuilder(length);

        for (int i = 0; i < length; i++) {
            var randomIndex = random.Next(chars.Length);
            passwordBuilder.Append(chars[randomIndex]);
        }

        return passwordBuilder.ToString();
    }

    public static string EncryptPassword(in string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public static bool VerifyPassword(in string password, in string hashedPassword) {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword)) return false;

        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    public static async Task<bool> AreReservationsEnabledAsync() {
        bool r = bool.TryParse(await Database.GetDataAsync("enableReservations") as string, out bool result) && result;

        return r;
    }

    public static bool IsPasswordValid(in string password) {
        if (string.IsNullOrEmpty(password)) return false;

        if (password.Length < 8) return false;

        if (!password.Any(char.IsUpper)) return false;

        if (!password.Any(char.IsLower)) return false;

        if (!password.Any(char.IsDigit)) return false;

        if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",.<>?/".Contains(c))) return false;

        return true;
    }

    public static bool AreReservationsEnabled() => AreReservationsEnabledAsync().Result;
}