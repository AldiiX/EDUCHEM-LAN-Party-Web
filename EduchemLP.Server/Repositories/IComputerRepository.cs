using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IComputerRepository {
    Task<List<Computer>> GetAllAsync(CancellationToken ct);
}