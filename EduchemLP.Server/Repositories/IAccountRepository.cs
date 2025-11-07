using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IAccountRepository {
    Task<Account?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Account>> GetAllAsync(CancellationToken ct = default);
    Task UpdateLastLoggedInAsync(Account account, CancellationToken ct = default);
    Task UpdateAvatarByConnectedPlatformAsync(Account account, CancellationToken ct = default);
    Task<Account?> CreateAsync(string email, string displayName, string? @class, Account.AccountGender gender, Account.AccountType accountType, bool sendToEmail = false, bool enableReservation = false, CancellationToken ct = default);
    Task<string?> GenerateDiscordAccessTokenAsync(Account account, CancellationToken ct = default);
    Task<string?> GenerateGoogleAccessTokenAsync(Account account, CancellationToken ct = default);
}