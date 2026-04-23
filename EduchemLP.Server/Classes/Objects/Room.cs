namespace EduchemLP.Server.Classes.Objects;




public class Room {
    private Room() {}

    public Room(string id, string label, string? image, UInt16 limitOfSeats) {
        Id = id;
        Label = label;
        Image = image;
        LimitOfSeats = limitOfSeats;
    }

    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Image { get; set; }
    public UInt16 LimitOfSeats { get; set; }
    public bool Available { get; set; } = true;
    public List<Computer> Computers { get; set; } = [];
}
