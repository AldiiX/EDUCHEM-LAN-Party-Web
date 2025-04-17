using System.Net.WebSockets;

namespace EduchemLP.Server.Classes.Objects;

public class WSClient
{
    
        public int ID { get; set; }
        public string DisplayName { get; set; }
        public string? Class { get; set; }
        public WebSocket WebSocket { get; set; }
        public string AccountType { get; set; }

        public WSClient(int id, string displayName, WebSocket webSocket, string accountType, string? @class) {
            ID = id;
            DisplayName = displayName;
            WebSocket = webSocket;
            Class = @class;
            AccountType = accountType;
        }
        
}