using System.Text.Json.Serialization;

namespace EduchemLP.Server.Classes;

public class LowerCaseJsonStringEnumConverter(): JsonStringEnumConverter(new LowerCaseNamingPolicy(), allowIntegerValues: true);