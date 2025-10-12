using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Services;

public interface IAuthService {
    Task<Account?> LoginAsync(string identifier, string plainPassword, CancellationToken ct = default);
    Task<Account?> ReAuthAsync(CancellationToken ct = default);
    Task<Account?> ReAuthFromContextOrNullAsync(CancellationToken ct = default);
    //Task<Account?> RegisterAsync(string username, string email, string plainPassword, string? displayName, CancellationToken ct = default);
}