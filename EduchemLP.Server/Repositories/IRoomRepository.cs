using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IRoomRepository {
    Task<List<Room>> GetAllAsync(CancellationToken ct);
}