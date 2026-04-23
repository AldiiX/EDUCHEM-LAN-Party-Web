namespace EduchemLP.Server.Data.Entities;

public class AppSetting {
    private AppSetting() {}

    public AppSetting(string property, string? value) {
        Property = property;
        Value = value;
    }

    public string Property { get; set; } = null!;
    public string? Value { get; set; }
}
