using System.Globalization;
using MySqlConnector;

namespace EduchemLP.Server.Services;



public class AppSettingsService(IDatabaseService db) : IAppSettingsService {
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    // simple in-memory cache
    private DateTime? _reservationsEnabledFrom;
    private DateTime? _reservationsEnabledTo;
    private IAppSettingsService.ReservationStatusType? _reservationsStatus;
    private bool? _chatEnabled;

    public async Task<DateTime> GetReservationsEnabledFromAsync(CancellationToken ct = default) {
        if (_reservationsEnabledFrom.HasValue) return _reservationsEnabledFrom.Value;

        var s = await GetSettingRawAsync("reservations_enabled_from", ct);
        if (!string.IsNullOrWhiteSpace(s)) {
            if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                || DateTime.TryParse(s, out dt)) {
                _reservationsEnabledFrom = dt;
                return dt;
            }
        }

        _reservationsEnabledFrom = DateTime.MaxValue;
        return _reservationsEnabledFrom.Value;
    }

    public async Task SetReservationsEnabledFromAsync(DateTime value, CancellationToken ct = default) {
        var s = value.ToString(DateFormat, CultureInfo.InvariantCulture);
        await UpsertSettingRawAsync("reservations_enabled_from", s, ct);
        _reservationsEnabledFrom = value;
    }

    public async Task<DateTime> GetReservationsEnabledToAsync(CancellationToken ct = default) {
        if (_reservationsEnabledTo.HasValue) return _reservationsEnabledTo.Value;

        var s = await GetSettingRawAsync("reservations_enabled_to", ct);
        if (!string.IsNullOrWhiteSpace(s)) {
            if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                || DateTime.TryParse(s, out dt)) {
                _reservationsEnabledTo = dt;
                return dt;
            }
        }

        _reservationsEnabledTo = DateTime.MaxValue;
        return _reservationsEnabledTo.Value;
    }

    public async Task SetReservationsEnabledToAsync(DateTime value, CancellationToken ct = default) {
        var s = value.ToString(DateFormat, CultureInfo.InvariantCulture);
        await UpsertSettingRawAsync("reservations_enabled_to", s, ct);
        _reservationsEnabledTo = value;
    }

    public async Task<IAppSettingsService.ReservationStatusType> GetReservationsStatusAsync(CancellationToken ct = default) {
        if (_reservationsStatus.HasValue) return _reservationsStatus.Value;

        var s = await GetSettingRawAsync("reservations_status", ct);
        if (!string.IsNullOrWhiteSpace(s)
            && Enum.TryParse(s, out IAppSettingsService.ReservationStatusType status)) {
            _reservationsStatus = status;
            return status;
        }

        _reservationsStatus = IAppSettingsService.ReservationStatusType.CLOSED;
        return _reservationsStatus.Value;
    }

    public async Task SetReservationsStatusAsync(IAppSettingsService.ReservationStatusType value, CancellationToken ct = default) {
        await UpsertSettingRawAsync("reservations_status", value.ToString(), ct);
        _reservationsStatus = value;
    }

    public async Task<bool> GetChatEnabledAsync(CancellationToken ct = default) {
        if (_chatEnabled.HasValue) return _chatEnabled.Value;

        var s = await GetSettingRawAsync("chat_enabled", ct);
        if (!string.IsNullOrWhiteSpace(s) && bool.TryParse(s, out var b)) {
            _chatEnabled = b;
            return b;
        }

        _chatEnabled = false;
        return false;
    }

    public async Task SetChatEnabledAsync(bool value, CancellationToken ct = default) {
        await UpsertSettingRawAsync("chat_enabled", value.ToString(), ct);
        _chatEnabled = value;
    }

    public async Task<bool> AreReservationsEnabledRightNowAsync(CancellationToken ct = default) {
        var status = await GetReservationsStatusAsync(ct);
        if (status == IAppSettingsService.ReservationStatusType.CLOSED) return false;
        if (status == IAppSettingsService.ReservationStatusType.OPEN) return true;

        var from = await GetReservationsEnabledFromAsync(ct);
        var to = await GetReservationsEnabledToAsync(ct);
        var now = DateTime.UtcNow;
        return now >= from && now <= to;
    }



    // helpers

    private async Task<string?> GetSettingRawAsync(string key, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);
        await using var cmd = new MySqlCommand("SELECT `value` FROM `settings` WHERE `property` = @key LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@key", key);
        var obj = await cmd.ExecuteScalarAsync(ct);
        return obj?.ToString();
    }

    private async Task UpsertSettingRawAsync(string key, string value, CancellationToken ct) {
        await using var conn = await db.GetOpenConnectionAsync(ct);

        // nejprve update, kdyz nezasahne radek, tak insert
        await using (var upd = new MySqlCommand("UPDATE `settings` SET `value` = @value WHERE `property` = @key", conn)) {
            upd.Parameters.AddWithValue("@key", key);
            upd.Parameters.AddWithValue("@value", value);
            var affected = await upd.ExecuteNonQueryAsync(ct);
            if (affected > 0) return;
        }

        await using (var ins = new MySqlCommand("INSERT INTO `settings` (`property`, `value`) VALUES (@key, @value)", conn)) {
            ins.Parameters.AddWithValue("@key", key);
            ins.Parameters.AddWithValue("@value", value);
            await ins.ExecuteNonQueryAsync(ct);
        }
    }
}