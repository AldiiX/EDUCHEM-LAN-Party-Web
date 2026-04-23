using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.DTOs;

public abstract record BaseDto;

public abstract record EntityDto<TId> : BaseDto {
    public required TId Id { get; init; }
}

public record AccountDto : EntityDto<int> {
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public string? Class { get; init; }
    public string? Avatar { get; init; }
    public string? Banner { get; init; }
    public required Account.AccountType Type { get; init; }
    public Account.AccountGender? Gender { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastUpdated { get; init; }
    public DateTime? LastLoggedIn { get; init; }
    public required bool EnableReservation { get; init; }
}

public record AccountPublicDto : AccountDto {
    public required IReadOnlyList<AccountAccessTokenDto> AccessTokens { get; init; }
}

public record AccountAccessTokenDto : BaseDto {
    public required int UserId { get; init; }
    public required Account.AccountAccessToken.AccountAccessTokenPlatform Platform { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public required Account.AccountAccessToken.AccountAccessTokenType Type { get; init; }
}

public record RoomDto : EntityDto<string> {
    public required string Label { get; init; }
    public string? Image { get; init; }
    public required ushort LimitOfSeats { get; init; }
    public required bool Available { get; init; }
}

public record ComputerDto : EntityDto<string> {
    public string? RoomId { get; init; }
    public string? Image { get; init; }
    public required bool IsTeacherPC { get; init; }
    public required bool Available { get; init; }
}
