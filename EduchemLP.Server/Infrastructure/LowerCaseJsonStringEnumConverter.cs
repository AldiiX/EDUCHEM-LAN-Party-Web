using System.Text.Json.Serialization;

namespace EduchemLP.Server.Infrastructure;

public class LowerCaseJsonStringEnumConverter(): JsonStringEnumConverter(new LowerCaseNamingPolicy(), allowIntegerValues: true);
