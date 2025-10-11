namespace EduchemLP.Server.Classes.Objects;





public class Computer {
    public Computer(string id, bool isTeacherPC, string? image) {
        Id = id;
        IsTeacherPC = isTeacherPC;
        Image = image;
    }

    public string Id { get; private set; }
    public string? Image { get; private set; }
    public bool IsTeacherPC { get; private set; }
}