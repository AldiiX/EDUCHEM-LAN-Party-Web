using System.Text.Json;

namespace EduchemLP.Server.Classes;

public class LowerCaseNamingPolicy : JsonNamingPolicy {
    public override string ConvertName(string name) => name.ToLowerInvariant();
}