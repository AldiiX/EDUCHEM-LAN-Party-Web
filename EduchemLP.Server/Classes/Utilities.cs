using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EduchemLP.Server.Services;

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

    public static V? GetValueOrNull<K, V>(this IDictionary<K, V> dictionary, K key) {
        return dictionary.TryGetValue(key, out var value) ? value : default;
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

        try {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (BCrypt.Net.SaltParseException) {
            // If we get a salt parse exception, return false instead of throwing
            return false;
        }
    }

    public static bool IsPasswordValid(in string password) {
        if (string.IsNullOrEmpty(password)) return false;

        if (password.Length < 8) return false;

        if (!password.Any(char.IsUpper)) return false;

        if (!password.Any(char.IsLower)) return false;

        if (!password.Any(char.IsDigit)) return false;

        if (!password.Any(c => "ěščřžýáíéůú!@#$%^&*()_+-=[]{}|;':\",.<>?/".Contains(c))) return false;

        return true;
    }

    public static string ToJsonString(this object obj) {
        return JsonSerializer.Serialize(obj, JsonSerializerOptions.Web);
    }

    public static JsonNode ToJsonNode(this object obj, in JsonSerializerOptions? options = null) {
        return JsonSerializer.SerializeToNode(obj, options ?? JsonSerializerOptions.Web) ?? new JsonObject();
    }
}