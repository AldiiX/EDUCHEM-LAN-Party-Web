/*
 *
 *  6000 připojených socketů je otestováno, že funguje bez problémů.
 *
 *  Nad 6000 už začíná být aplikace zpomalená (pravděpodobně kvůli nedostatku paměti).
 *
 *  Pravděpodobně nikdy nebude potřeba více než 6000 současných připojení, proto prozatím není
 *  potřebné řešit škálování na více serverů.
 *
 */

using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using EduchemLP.Server.Data;
using EduchemLP.Server.Data.Entities;
using EduchemLP.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.WebSockets;

public sealed class ReservationsClient : WSClient {
    public string DisplayName { get; }
    public Account.AccountType? AccountType { get; }
    public string? Class { get; }
    public string? Avatar { get; }
    public string? Banner { get; }
    public bool IsGuest { get; }

    public ReservationsClient(WebSocket socket, Account? account) : base(socket) {
        if (account is null) {
            var rnd = (uint)Random.Shared.Next(10_000, 999_999);
            Id = rnd;
            DisplayName = "Guest";
            AccountType = null;
            IsGuest = true;
        } else {
            Id = (uint)account.Id;
            DisplayName = account.DisplayName;
            AccountType = account.Type;
            Class = account.Class;
            Avatar = account.Avatar;
            Banner = account.Banner;
            IsGuest = false;
        }
    }
}

internal sealed record ReservationRow(
    int? UserId,
    string? UserDisplayName,
    string? UserClass,
    string? UserAvatar,
    string? UserBanner,
    string? RoomId,
    string? RoomLabel,
    int? RoomLimitOfSeats,
    bool? RoomAvailable,
    string? RoomImage,
    string? ComputerId,
    bool? ComputerIsTeachersPc,
    bool? ComputerAvailable,
    string? ComputerImage,
    string? Note,
    DateTime CreatedAt
);

internal sealed record ReservationSnapshot(
    IReadOnlyList<ReservationRow> Reservations,
    object Rooms,
    object Computers
);

public sealed class ReservationsWebSocketEndpoint(
    EduchemLpDbContext orm,
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationsWebSocketEndpoint> logger,
    IDbLoggerService dbLogger,
    IWebSocketHub hub
) : IWebSocketEndpoint {

    public PathString Path => "/ws/reservations";

    private static int heartbeatRegistered = 0;

    private ReservationSnapshot? _currentSnapshot;
    private readonly SemaphoreSlim _snapshotSemaphore = new(1, 1);

    private static readonly TimeSpan StatusBroadcastInterval = TimeSpan.FromSeconds(1);
    private static readonly Lock _statusBroadcastLock = new();
    private static bool _statusBroadcastScheduled;

    private static readonly Lock _statusCacheLock = new();
    private static DateTime _statusCacheValidUntilUtc = DateTime.MinValue;
    private static string? _statusPayloadForGuest;
    private static string? _statusPayloadForAuthenticated;

    public async Task HandleAsync(HttpContext context, WebSocket socket, CancellationToken ct = default) {
        using var scope = scopeFactory.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var sessionAccount = await auth.ReAuthAsync(ct);
        var client = new ReservationsClient(socket, sessionAccount);

        hub.AddClient("reservations", client);
        await BroadcastStatusAsync(ct);

        var snapshot = await GetOrLoadSnapshotAsync(ct);
        await SendFullReservationInfoAsync(client, snapshot, ct);

        await RegisterHeartbeatAsync();

        var buffer = new byte[4 * 1024];
        try {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open) {
                WebSocketReceiveResult result;
                try {
                    result = await socket.ReceiveAsync(buffer, ct);
                } catch {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close) {
                    break;
                }

                var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (string.IsNullOrWhiteSpace(messageString)) continue;

                JsonNode? messageJson;
                try {
                    messageJson = JsonNode.Parse(messageString);
                } catch {
                    continue;
                }

                var action = messageJson?["action"]?.ToString();
                if (action is null) continue;

                if (sessionAccount is null && action != "disconnect") {
                    await client.SendAsync(new {
                            action = "error",
                            message = "Nejsi přihlášen."
                        }.ToJsonString(), ct
                    );
                    continue;
                }

                switch (action) {
                    case "reserve": {
                        using var reserveScope = scopeFactory.CreateScope();
                        var freshAppSettings = reserveScope.ServiceProvider.GetRequiredService<IAppSettingsService>();

                        if (!await freshAppSettings.AreReservationsEnabledRightNowAsync(ct)) {
                            await client.SendAsync(new {
                                    action = "error",
                                    message = "Rezervace nejsou momentálně povolené."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        var accountCanReserve = await IsAccountAbleToReserveAsync(sessionAccount, ct);
                        if (!accountCanReserve) {
                            await client.SendAsync(new {
                                    action = "error",
                                    message = "Tvůj účet nemá povolené rezervace."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        var room = messageJson?["room"]?.ToString();
                        var computer = messageJson?["computer"]?.ToString();
                        if (string.IsNullOrWhiteSpace(room) && string.IsNullOrWhiteSpace(computer)) break;

                        var existing = await orm.Reservations.FirstOrDefaultAsync(x => x.UserId == sessionAccount!.Id, ct);
                        if (existing != null) {
                            orm.Reservations.Remove(existing);
                        }

                        orm.Reservations.Add(new Reservation(sessionAccount!.Id,
                            string.IsNullOrWhiteSpace(room) ? null : room,
                            string.IsNullOrWhiteSpace(computer) ? null : computer));

                        await orm.SaveChangesAsync(ct);

                        await ReloadAndBroadcastFullReservationInfoAsync(ct);

                        _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO,
                            $"Uživatel {sessionAccount.DisplayName} ({sessionAccount.Email}) rezervoval {room ?? computer}.",
                            "reservation", ct
                        );
                    } break;

                    case "deleteReservation": {
                        using var deleteScope = scopeFactory.CreateScope();
                        var freshAppSettings = deleteScope.ServiceProvider.GetRequiredService<IAppSettingsService>();

                        if (!await freshAppSettings.AreReservationsEnabledRightNowAsync(ct)) {
                            await client.SendAsync(new {
                                    action = "error",
                                    message = "Rezervace nejsou momentálně povolené."
                                }.ToJsonString(), ct
                            );

                            continue;
                        }

                        var existing = await orm.Reservations.FirstOrDefaultAsync(x => x.UserId == sessionAccount!.Id, ct);
                        if (existing != null) {
                            orm.Reservations.Remove(existing);
                            await orm.SaveChangesAsync(ct);
                        }

                        await ReloadAndBroadcastFullReservationInfoAsync(ct);

                        _ = dbLogger.LogAsync(IDbLoggerService.LogType.INFO,
                            $"Uživatel {sessionAccount.DisplayName} ({sessionAccount.Email}) zrušil rezervaci.",
                            "reservation", ct
                        );
                    } break;

                    case "disconnect": {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", ct);
                    } break;
                }
            }
        }
        finally {
            hub.RemoveClient("reservations", client.Id);
            await BroadcastStatusAsync(CancellationToken.None);
        }
    }

    private async Task<ReservationSnapshot> GetOrLoadSnapshotAsync(CancellationToken ct) {
        var existing = _currentSnapshot;
        if (existing is not null) return existing;

        await _snapshotSemaphore.WaitAsync(ct);
        try {
            if (_currentSnapshot is not null) return _currentSnapshot;

            _currentSnapshot = await LoadReservationSnapshotCoreAsync(ct);
            return _currentSnapshot;
        }
        finally {
            _snapshotSemaphore.Release();
        }
    }

    private async Task ReloadAndBroadcastFullReservationInfoAsync(CancellationToken ct) {
        ReservationSnapshot snapshot;

        await _snapshotSemaphore.WaitAsync(ct);
        try {
            snapshot = await LoadReservationSnapshotCoreAsync(ct);
            _currentSnapshot = snapshot;
        }
        finally {
            _snapshotSemaphore.Release();
        }

        var clients = hub.GetClients("reservations").ToList().Cast<ReservationsClient>();
        foreach (var c in clients) {
            if (c.State != WebSocketState.Open) continue;
            await SendFullReservationInfoAsync(c, snapshot, ct);
        }
    }

    private async Task<ReservationSnapshot> LoadReservationSnapshotCoreAsync(CancellationToken ct) {
        var roomsLocal = await orm.Rooms
            .AsNoTracking()
            .Where(room => room.Available)
            .OrderBy(room => room.Label)
            .ToListAsync(ct);

        var computersLocal = await orm.Computers
            .AsNoTracking()
            .Include(computer => computer.Room)
            .Where(computer => computer.Available)
            .OrderBy(computer => computer.Id)
            .Select(computer => new Computer(computer.Id, computer.IsTeacherPC, computer.Room != null ? computer.Room.Image : null))
            .ToListAsync(ct);

        var reservationEntities = await orm.Reservations
            .AsNoTracking()
            .Include(res => res.User)
            .Include(res => res.Room)
            .Include(res => res.Computer)
                .ThenInclude(comp => comp!.Room)
            .Where(res => (res.Computer != null && res.Computer.Available) || (res.Room != null && res.Room.Available))
            .OrderByDescending(res => res.CreatedAt)
            .ToListAsync(ct);

        var reservations = reservationEntities.Select(res => new ReservationRow(
            UserId: res.User?.Id,
            UserDisplayName: res.User?.DisplayName,
            UserClass: res.User?.Class,
            UserAvatar: res.User?.Avatar,
            UserBanner: res.User?.Banner,
            RoomId: res.Room?.Id,
            RoomLabel: res.Room?.Label ?? res.Room?.Id,
            RoomLimitOfSeats: res.Room is null ? null : res.Room.LimitOfSeats,
            RoomAvailable: res.Room?.Available,
            RoomImage: res.Room?.Image,
            ComputerId: res.Computer?.Id,
            ComputerIsTeachersPc: res.Computer?.IsTeacherPC,
            ComputerAvailable: res.Computer?.Available,
            ComputerImage: res.Computer?.Room?.Image,
            Note: res.Note,
            CreatedAt: res.CreatedAt
        )).ToList();

        return new ReservationSnapshot(reservations, roomsLocal, computersLocal);
    }

    private Task BroadcastStatusAsync(CancellationToken ct) {
        lock (_statusBroadcastLock) {
            if (_statusBroadcastScheduled) {
                return Task.CompletedTask;
            }

            _statusBroadcastScheduled = true;

            _ = Task.Run(async () => {
                try {
                    try {
                        await Task.Delay(StatusBroadcastInterval, ct);
                    }
                    catch (OperationCanceledException) {
                    }

                    await BroadcastStatusCoreAsync(CancellationToken.None);
                }
                finally {
                    lock (_statusBroadcastLock) {
                        _statusBroadcastScheduled = false;
                    }
                }
            }, CancellationToken.None);

            return Task.CompletedTask;
        }
    }

    private async Task BroadcastStatusCoreAsync(CancellationToken ct) {
        var list = hub.GetClients("reservations").ToList();

        foreach (var r in list) {
            if (r.State != WebSocketState.Open) continue;

            var receiver = (ReservationsClient)r;
            var payload = await BuildStatusPayloadAsync(receiver, ct);
            await hub.SendAsync("reservations", receiver.Id, payload, ct);
        }
    }

    private async Task SendFullReservationInfoAsync(ReservationsClient client, ReservationSnapshot snapshot, CancellationToken ct) {
        var reservationsJson = new JsonArray();

        foreach (var row in snapshot.Reservations) {
            JsonNode? userNode;

            if (client.IsGuest) {
                userNode = "unknown";
            } else if (row.UserId is null) {
                userNode = null;
            } else {
                var userObj = new JsonObject {
                    ["id"] = row.UserId,
                    ["displayName"] = row.UserDisplayName,
                    ["avatar"] = row.UserAvatar,
                    ["banner"] = row.UserBanner
                };

                if (client.AccountType is > Account.AccountType.STUDENT) {
                    userObj["class"] = row.UserClass;
                }

                userNode = userObj;
            }

            JsonObject? roomObj = null;
            if (!string.IsNullOrEmpty(row.RoomId)) {
                roomObj = new JsonObject {
                    ["id"] = row.RoomId,
                    ["label"] = row.RoomLabel ?? row.RoomId,
                    ["limitOfSeats"] = row.RoomLimitOfSeats,
                    ["available"] = row.RoomAvailable,
                    ["image"] = row.RoomImage
                };
            }

            JsonObject? compObj = null;
            if (!string.IsNullOrEmpty(row.ComputerId)) {
                compObj = new JsonObject {
                    ["id"] = row.ComputerId,
                    ["isTeachersPC"] = row.ComputerIsTeachersPc,
                    ["available"] = row.ComputerAvailable,
                    ["image"] = row.ComputerImage
                };
            }

            var item = new JsonObject {
                ["user"] = userNode,
                ["room"] = roomObj,
                ["computer"] = compObj,
                ["note"] = row.Note is null ? null : JsonValue.Create(row.Note),
                ["createdAt"] = row.CreatedAt
            };

            reservationsJson.Add(item);
        }

        var payload = new {
            action = "fetchAll",
            reservations = reservationsJson,
            computers = snapshot.Computers,
            rooms = snapshot.Rooms
        }.ToJsonString();

        await hub.SendAsync("reservations", client.Id, payload, ct);
    }

    private async Task RegisterHeartbeatAsync() {
        if (Interlocked.CompareExchange(ref heartbeatRegistered, 1, 0) == 0) {
            hub.RegisterHeartbeat("reservations", async (_, token) => {
                await BroadcastStatusAsync(token);
            });
        }
    }

    private async Task<bool> IsAccountAbleToReserveAsync(Account? account, CancellationToken ct) {
        if (account == null) return false;

        return await orm.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == account.Id && x.EnableReservation, ct);
    }

    private Task<string> BuildStatusPayloadAsync(ReservationsClient receiver, CancellationToken ct) {
        var now = DateTime.UtcNow;

        lock (_statusCacheLock) {
            if (now <= _statusCacheValidUntilUtc) {
                if (receiver.AccountType is null && _statusPayloadForGuest is not null) {
                    return Task.FromResult(_statusPayloadForGuest);
                }

                if (receiver.AccountType is not null && _statusPayloadForAuthenticated is not null) {
                    return Task.FromResult(_statusPayloadForAuthenticated);
                }
            }
        }

        var list = hub.GetClients("reservations").ToList();
        var connectedUsers = new JsonArray();

        foreach (var client in list.DistinctBy(x => x.Id)) {
            var c = (ReservationsClient)client;

            if (c.AccountType is null || receiver.AccountType is null) {
                connectedUsers.Add("unknown");
                continue;
            }

            connectedUsers.Add(new JsonObject {
                ["id"] = c.Id,
                ["displayName"] = c.DisplayName,
                ["class"] = c.AccountType > Account.AccountType.STUDENT ? c.Class : null
            });
        }

        var json = new {
            action = "status",
            connectedUsers
        }.ToJsonString();

        lock (_statusCacheLock) {
            _statusCacheValidUntilUtc = DateTime.UtcNow.Add(StatusBroadcastInterval);

            if (receiver.AccountType is null) {
                _statusPayloadForGuest = json;
            } else {
                _statusPayloadForAuthenticated = json;
            }
        }

        return Task.FromResult(json);
    }
}
