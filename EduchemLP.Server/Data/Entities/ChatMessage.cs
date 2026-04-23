namespace EduchemLP.Server.Data.Entities;

public class ChatMessage {
    private ChatMessage() {}

    public ChatMessage(string uuid, int userId, string message, string? replyingToUuid) {
        Uuid = uuid;
        UserId = userId;
        Message = message;
        ReplyingToUuid = replyingToUuid;
    }

    public string Uuid { get; set; } = null!;
    public int UserId { get; set; }
    public Account? User { get; set; }
    public string Message { get; set; } = null!;
    public DateTime Date { get; set; }
    public bool Deleted { get; set; }
    public string? ReplyingToUuid { get; set; }
}
