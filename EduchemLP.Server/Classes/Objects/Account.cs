using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using EduchemLP.Server.Infrastructure;
using MySqlConnector;

namespace EduchemLP.Server.Classes.Objects;

public partial class Account {
    public int Id { get; private set; }
    public string DisplayName { get; private set; }

    [Obsolete("Use DisplayName")]
    public string Name => DisplayName;

    public string Email { get; private set; }
    public string Password { get; private set; }
    public string? Class { get; private set; }
    public string? Avatar { get; private set; }
    public string? Banner { get; private set; }
    public AccountType Type { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public DateTime? LastLoggedIn { get; private set; }
    public AccountGender? Gender { get; private set; }
    public List<AccountAccessToken> AccessTokens { get; private set; }

    public bool EnableReservation { get; private set; }



    [JsonConstructor]
    public Account(int id, string displayName, string email, string password, string? @class, AccountType type, DateTime lastUpdated, DateTime? lastLoggedIn, AccountGender? gender, string? avatar, string? banner, List<AccountAccessToken>? accessTokens, bool enableReservation = false) {
        Id = id;
        DisplayName = displayName;
        Class = @class;
        Password = password;
        Email = email;
        Type = type;
        LastUpdated = lastUpdated;
        LastLoggedIn = lastLoggedIn;
        Avatar = avatar;
        Banner = banner;
        Gender = gender;
        AccessTokens = accessTokens ?? [];
        EnableReservation = enableReservation;
    }

    public Account(MySqlDataReader reader) : this(
        reader.GetInt32("id"),
        reader.GetString("display_name"),
        reader.GetString("email"),
        reader.GetString("password"),
        reader.GetStringOrNull("class"),
        Enum.TryParse(reader.GetString("account_type"), out Account.AccountType _ac) ? _ac : Account.AccountType.STUDENT,
        reader.GetDateTime("last_updated"),
        reader.GetValueOrNull<DateTime>("last_logged_in"),
        Enum.TryParse<Account.AccountGender>(reader.GetStringOrNull("gender"), out var _g ) ? _g : null,
        reader.GetStringOrNull("avatar"),
        reader.GetStringOrNull("banner"),
        JsonSerializer.Deserialize<List<Account.AccountAccessToken>>(reader.GetStringOrNull("access_tokens") ?? "[]", JsonSerializerOptions.Web) ?? [],
        reader.GetBoolean("enable_reservation")
    ) {}



    public override string ToString() {
        return JsonSerializer.Serialize(this, JsonSerializerOptions.Web);
    }

    /// <summary>
    /// Metoda, která vrací JSON reprezentaci uživatele s veřejnými údaji.
    /// </summary>
    /// <returns><see cref="JsonNode"/> s veřejnými údaji o accountu (tj. bez hesel, klíčů atd.)</returns>
    public JsonNode ToPublicJsonNode() {
        var obj = this.ToJsonNode(JsonSerializerOptions.Web);

        // přidání connections
        var arr = new JsonArray();

        foreach (var token in AccessTokens) {
            arr.Add(token.Platform.ToString().ToUpper());
        }

        obj["connections"] = arr;
        obj.AsObject().Remove("password");
        obj.AsObject().Remove("accessTokens");

        return obj;
    }
}

public partial class Account {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccountGender { MALE, FEMALE, OTHER}

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccountType {
        STUDENT,
        TEACHER,
        ADMIN,
        SUPERADMIN,
    }

    public sealed class AccountAccessToken {
        public enum AccountAccessTokenPlatform {
            DISCORD,
            GOOGLE,
            FACEBOOK,
            TWITTER,
            GITHUB,
            INSTAGRAM
        }

        public enum AccountAccessTokenType {
            BEARER,
        }


        [JsonConstructor]
        public AccountAccessToken (int userId, AccountAccessTokenPlatform platform, string? accessToken, string? refreshToken, AccountAccessTokenType type) {
            UserId = userId;
            Platform = platform;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            Type = type;
        }

        public int UserId { get; private set; }
        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccountAccessTokenPlatform Platform { get; private set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccountAccessTokenType Type { get; private set; }
    }
}