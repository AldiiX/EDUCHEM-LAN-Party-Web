namespace EduchemLP.Server.Data.Entities;

public class Computer {
    private Computer() {}

    public Computer(string id, bool isTeacherPC, string? image) {
        Id = id;
        IsTeacherPC = isTeacherPC;
        Image = image;
    }

    public string Id { get; set; } = null!;
    public string? RoomId { get; set; }
    public Room? Room { get; set; }
    public string? Image { get; set; }
    public bool IsTeacherPC { get; set; }
    public bool Available { get; set; } = true;
}
