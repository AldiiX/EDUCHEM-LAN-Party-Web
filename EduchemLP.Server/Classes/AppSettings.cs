using MySql.Data.MySqlClient;

namespace EduchemLP.Server.Classes;

public static class AppSettings {
    public enum ReservationStatusType {
        USE_TIMER,
        OPEN,
        CLOSED
    }

    // private props
    private static ReservationStatusType? _reservationsStatus;
    private static DateTime? _reservationsEnabledFrom;
    private static DateTime? _reservationsEnabledTo;
    private static bool? _chatEnabled;


    // public props
    public static DateTime ReservationsEnabledFrom {
        get {
            if(_reservationsEnabledFrom != null) return _reservationsEnabledFrom.Value;

            using var conn = Database.GetConnection();
            if(conn == null) return DateTime.MaxValue;

            var cmd = new MySqlCommand("SELECT `value` FROM `settings` WHERE `property` = 'reservations_enabled_from'", conn);
            var reader = cmd.ExecuteReader();

            if(reader.Read()) {
                var value = reader.GetString(0);
                if(DateTime.TryParse(value, out var dateTime)) {
                    _reservationsEnabledFrom = dateTime;
                    return dateTime;
                }
            }

            _reservationsEnabledFrom = DateTime.MaxValue;
            return _reservationsEnabledFrom.Value;
        }

        set {
            using var conn = Database.GetConnection();
            if(conn == null) return;

            var cmd = new MySqlCommand("UPDATE `settings` SET `value` = @value WHERE `property` = 'reservations_enabled_from'", conn);
            cmd.Parameters.AddWithValue("@value", value.ToString("yyyy-MM-dd HH:mm:ss"));

            if(cmd.ExecuteNonQuery() == 0) {
                cmd = new MySqlCommand("INSERT INTO `settings` (`property`, `value`) VALUES ('reservations_enabled_from', @value)", conn);
                cmd.Parameters.AddWithValue("@value", value.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }

            _reservationsEnabledFrom = value;
        }
    }
    public static DateTime ReservationsEnabledTo {
        get {
            if(_reservationsEnabledTo != null) return _reservationsEnabledTo.Value;

            using var conn = Database.GetConnection();
            if(conn == null) return DateTime.MaxValue;

            var cmd = new MySqlCommand("SELECT `value` FROM `settings` WHERE `property` = 'reservations_enabled_to'", conn);
            var reader = cmd.ExecuteReader();

            if(reader.Read()) {
                var value = reader.GetString(0);
                if(DateTime.TryParse(value, out var dateTime)) {
                    _reservationsEnabledTo = dateTime;
                    return dateTime;
                }
            }

            _reservationsEnabledTo = DateTime.MaxValue;
            return _reservationsEnabledTo.Value;
        }

        set {
            using var conn = Database.GetConnection();
            if(conn == null) return;

            var cmd = new MySqlCommand("UPDATE `settings` SET `value` = @value WHERE `property` = 'reservations_enabled_to'", conn);
            cmd.Parameters.AddWithValue("@value", value.ToString("yyyy-MM-dd HH:mm:ss"));

            if(cmd.ExecuteNonQuery() == 0) {
                cmd = new MySqlCommand("INSERT INTO `settings` (`property`, `value`) VALUES ('reservations_enabled_to', @value)", conn);
                cmd.Parameters.AddWithValue("@value", value.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }

            _reservationsEnabledTo = value;
        }
    }
    public static ReservationStatusType ReservationsStatus {
        get {
            if(_reservationsStatus != null) return _reservationsStatus.Value;

            using var conn = Database.GetConnection();
            if(conn == null) return ReservationStatusType.CLOSED;

            var cmd = new MySqlCommand("SELECT `value` FROM `settings` WHERE `property` = 'reservations_status'", conn);
            var reader = cmd.ExecuteReader();

            if(reader.Read()) {
                var value = reader.GetString(0);
                if(Enum.TryParse(value, out ReservationStatusType status)) {
                    _reservationsStatus = status;
                    return status;
                }
            }

            _reservationsStatus = ReservationStatusType.CLOSED;
            return _reservationsStatus.Value;
        }

        set {
            using var conn = Database.GetConnection();
            if(conn == null) return;

            var cmd = new MySqlCommand("UPDATE `settings` SET `value` = @value WHERE `property` = 'reservations_status'", conn);
            cmd.Parameters.AddWithValue("@value", value.ToString());

            if(cmd.ExecuteNonQuery() == 0) {
                cmd = new MySqlCommand("INSERT INTO `settings` (`property`, `value`) VALUES ('reservations_status', @value)", conn);
                cmd.Parameters.AddWithValue("@value", value.ToString());
                cmd.ExecuteNonQuery();
            }

            _reservationsStatus = value;
        }
    }
    public static bool AreReservationsEnabledRightNow => ReservationsStatus == ReservationStatusType.OPEN || (ReservationsStatus == ReservationStatusType.USE_TIMER && DateTime.Now >= ReservationsEnabledFrom && DateTime.Now <= ReservationsEnabledTo);
    public static bool ChatEnabled {
        get {
            if(_chatEnabled != null) return _chatEnabled.Value;

            using var conn = Database.GetConnection();
            if(conn == null) return false;

            var cmd = new MySqlCommand("SELECT `value` FROM `settings` WHERE `property` = 'chat_enabled'", conn);
            var reader = cmd.ExecuteReader();

            if(reader.Read()) {
                var value = reader.GetString(0);
                if(bool.TryParse(value, out var enabled)) {
                    _chatEnabled = enabled;
                    return enabled;
                }
            }

            _chatEnabled = false;
            return _chatEnabled.Value;
        }

        set {
            using var conn = Database.GetConnection();
            if(conn == null) return;

            var cmd = new MySqlCommand("UPDATE `settings` SET `value` = @value WHERE `property` = 'chat_enabled'", conn);
            cmd.Parameters.AddWithValue("@value", value.ToString());

            if(cmd.ExecuteNonQuery() == 0) {
                cmd = new MySqlCommand("INSERT INTO `settings` (`property`, `value`) VALUES ('chat_enabled', @value)", conn);
                cmd.Parameters.AddWithValue("@value", value.ToString());
                cmd.ExecuteNonQuery();
            }

            _chatEnabled = value;
        }
    }
}