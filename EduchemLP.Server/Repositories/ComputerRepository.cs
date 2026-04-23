using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.Repositories;

public class ComputerRepository(EduchemLpDbContext db) : IComputerRepository {

    public async Task<List<Computer>> GetAllAsync(CancellationToken ct) {
        var rows = await db.Computers
            .AsNoTracking()
            .Include(computer => computer.Room)
            .Where(computer => computer.Available)
            .OrderBy(computer => computer.Id)
            .Select(computer => new {
                computer.Id,
                computer.IsTeacherPC,
                Image = computer.Room != null ? computer.Room.Image : null
            })
            .ToListAsync(ct);

        return rows.Select(computer =>
            new Computer(computer.Id, computer.IsTeacherPC, computer.Image)
        ).ToList();
    }
}
