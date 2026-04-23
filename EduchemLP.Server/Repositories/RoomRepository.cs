using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace EduchemLP.Server.Repositories;



public class RoomRepository(EduchemLpDbContext db) : IRoomRepository {

    public async Task<List<Room>> GetAllAsync(CancellationToken ct) {
        return await db.Rooms
            .AsNoTracking()
            .Where(room => room.Available)
            .OrderBy(room => room.Label)
            .ToListAsync(ct);
    }
}
