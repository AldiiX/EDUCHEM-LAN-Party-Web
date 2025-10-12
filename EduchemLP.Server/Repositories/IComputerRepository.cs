using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IComputerRepository {
    Task<List<Computer>> GetAllAsync(CancellationToken ct);
}