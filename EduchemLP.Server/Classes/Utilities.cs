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

    [Obsolete("Tato metoda se už nepoužívá, slouží jen jako předloha pro nové metody a aby byla uchována historie.", true)]
    public static string GenerateRandomAuthKey(int length = 48) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var keyBuilder = new StringBuilder(length);

        for (int i = 0; i < length; i++) {
            var randomIndex = random.Next(chars.Length);
            keyBuilder.Append(chars[randomIndex]);
        }

        return keyBuilder.ToString();
    }

    public static string GenerateRandomPassword(int length = 12) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
        var random = new Random();
        var passwordBuilder = new StringBuilder(length);

        for (int i = 0; i < length; i++) {
            var randomIndex = random.Next(chars.Length);
            passwordBuilder.Append(chars[randomIndex]);
        }

        return passwordBuilder.ToString();
    }

    public static string EncryptStringWithKey(string plainText, string key) {
        using Aes aes = Aes.Create();
        aes.Key = GenerateValidKey(key);
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = new byte[16];

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptStringWithKey(string encryptedText, string key) {
        using Aes aes = Aes.Create();
        aes.Key = GenerateValidKey(key);
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = new byte[16];

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
        byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] GenerateValidKey(string key) {
        using SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    private static string EncryptWithSHA512(in string password) {
        using SHA512 sha512 = SHA512.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] sha512HashBytes = sha512.ComputeHash(passwordBytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in sha512HashBytes) {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private static string EncryptWithMD5(in string password) {
        using MD5 md5 = MD5.Create();
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] md5HashBytes = md5.ComputeHash(passwordBytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in md5HashBytes) {
            sb.Append(b.ToString("x2"));
        }


        return sb.ToString();
    }

    public static string EncryptPassword(in string password) => EncryptWithSHA512(password) + EncryptWithMD5(password[0] + "" + password[1] + "" + password[^1]);

    public static async Task<bool> AreReservationsEnabledAsync() {
        bool r = bool.TryParse(await Database.GetDataAsync("enableReservations") as string, out bool result) && result;

        return r;
    }

    public static bool AreReservationsEnabled() => AreReservationsEnabledAsync().Result;
}