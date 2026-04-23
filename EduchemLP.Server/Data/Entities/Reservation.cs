namespace EduchemLP.Server.Data.Entities;

public class Reservation {
    private Reservation() {}

    public Reservation(int userId, string? roomId, string? computerId, string? note = null) {
        UserId = userId;
        RoomId = roomId;
        ComputerId = computerId;
        Note = note;
    }

    public int UserId { get; set; }
    public Account? User { get; set; }
    public string? RoomId { get; set; }
    public Room? Room { get; set; }
    public string? ComputerId { get; set; }
    public Computer? Computer { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
