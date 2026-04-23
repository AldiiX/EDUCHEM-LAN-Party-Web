namespace EduchemLP.Server.Data.Entities;

public class Log {
    public enum LogType { INFO, WARN, ERROR }

    private Log() {}

    public Log(LogType type, string exactType, string message) {
        Type = type;
        ExactType = exactType;
        Message = message;
    }

    public int Id { get; private set; }
    public LogType Type { get; set; }
    public string ExactType { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime Date { get; set; }
}
