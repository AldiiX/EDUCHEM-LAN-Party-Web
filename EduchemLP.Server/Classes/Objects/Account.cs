using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EduchemLP.Server.Classes.Objects;

public partial class Account {
    public int Id { get; private set; }
    public string DisplayName { get; set; } = null!;

    [Obsolete("Use DisplayName")]
    public string Name => DisplayName;

    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Class { get; set; }
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public AccountType Type { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdated { get; set; }
    public DateTime? LastLoggedIn { get; set; }
    public AccountGender? Gender { get; set; }
    public List<AccountAccessToken> AccessTokens { get; set; } = [];

    public bool EnableReservation { get; set; }



    private Account() {}

    [JsonConstructor]
    public Account(int id, string displayName, string email, string password, string? @class, AccountType type, DateTime createdAt, DateTime lastUpdated, DateTime? lastLoggedIn, AccountGender? gender, string? avatar, string? banner, List<AccountAccessToken>? accessTokens, bool enableReservation = false) {
        Id = id;
        DisplayName = displayName;
        Class = @class;
        Password = password;
        Email = email;
        Type = type;
        CreatedAt = createdAt;
        LastUpdated = lastUpdated;
        LastLoggedIn = lastLoggedIn;
        Avatar = avatar;
        Banner = banner;
        Gender = gender;
        AccessTokens = accessTokens ?? [];
        EnableReservation = enableReservation;
    }

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
        TEACHER_ORG,
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

        private AccountAccessToken() {}

        public int UserId { get; set; }
        public Account? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccountAccessTokenPlatform Platform { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AccountAccessTokenType Type { get; set; }
    }
}
