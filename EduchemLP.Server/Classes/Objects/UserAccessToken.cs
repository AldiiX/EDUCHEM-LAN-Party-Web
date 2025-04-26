using System.Text.Json.Serialization;

namespace EduchemLP.Server.Classes.Objects;





public partial class User {

    public sealed class UserAccessToken {
        public enum UserAccessTokenPlatform {
            DISCORD,
            GOOGLE,
            FACEBOOK,
            TWITTER,
            GITHUB,
        }

        public enum UserAccessTokenType {
            BEARER,
        }


        [JsonConstructor]
        public UserAccessToken (int userId, UserAccessTokenPlatform platform, string accessToken, string refreshToken, UserAccessTokenType type) {
            UserId = userId;
            Platform = platform;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            Type = type;
        }

        public int UserId { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserAccessTokenPlatform Platform { get; private set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserAccessTokenType Type { get; private set; }
    }
}