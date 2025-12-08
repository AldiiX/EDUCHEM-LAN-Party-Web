namespace EduchemLP.Server.Services;

public interface IDbLoggerService {
    public enum LogType { INFO, WARN, ERROR }

    Task<bool> LogAsync(LogType type, string message, string exactType = "basic", CancellationToken ct = default);
    Task<bool> LogErrorAsync(string message, string exactType = "basic", CancellationToken ct = default);
    Task<bool> LogInfoAsync(string message, string exactType = "basic", CancellationToken ct = default);
    Task<bool> LogWarnAsync(string message, string exactType = "basic", CancellationToken ct = default);
}