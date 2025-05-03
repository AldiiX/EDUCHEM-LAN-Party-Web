using System.Net.WebSockets;

namespace EduchemLP.Server.Classes.Objects;

public class WSClientUser : WSClient {
    public string? Class { get; set; }
    public User.UserAccountType? AccountType { get; set; }
    public string DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public int ID { get; set; }

    public WSClientUser(WebSocket webSocket, int id, string displayName, User.UserAccountType? accountType, string? @class, string? avatar, string? banner) : base(webSocket) {
        ID = id;
        DisplayName = displayName;
        AccountType = accountType;
        Class = @class;
        Avatar = avatar;
        Banner = banner;
    }
}