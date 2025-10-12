namespace EduchemLP.Server.Classes.Objects;




public class Room {
    public Room(string id, string label, string? image, UInt16 limitOfSeats) {
        Id = id;
        Label = label;
        Image = image;
        LimitOfSeats = limitOfSeats;
    }

    public string Id { get; private set; }
    public string Label { get; private set; }
    public string? Image { get; private set; }
    public UInt16 LimitOfSeats { get; private set; }
}