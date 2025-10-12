namespace EduchemLP.Server.Services;

public interface IAppSettingsService {
    public enum ReservationStatusType {
        USE_TIMER,
        OPEN,
        CLOSED
    }

    Task<DateTime> GetReservationsEnabledFromAsync(CancellationToken ct = default);
    Task SetReservationsEnabledFromAsync(DateTime value, CancellationToken ct = default);

    Task<DateTime> GetReservationsEnabledToAsync(CancellationToken ct = default);
    Task SetReservationsEnabledToAsync(DateTime value, CancellationToken ct = default);

    Task<ReservationStatusType> GetReservationsStatusAsync(CancellationToken ct = default);
    Task SetReservationsStatusAsync(ReservationStatusType value, CancellationToken ct = default);

    Task<bool> GetChatEnabledAsync(CancellationToken ct = default);
    Task SetChatEnabledAsync(bool value, CancellationToken ct = default);

    Task<bool> AreReservationsEnabledRightNowAsync(CancellationToken ct = default);
}