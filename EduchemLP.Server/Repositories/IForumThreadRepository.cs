using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IForumThreadRepository
{
    Task<IEnumerable<ForumThread>> GetAllThreadsAsync(CancellationToken ct = default);
    Task<ForumThread?> GetThreadByUuidAsync(Guid uuid, CancellationToken ct = default);
    Task<bool> CreateThreadAsync(ForumThread thread, CancellationToken ct = default);
    Task<bool> ApproveThreadAsync(Guid uuid, CancellationToken ct = default);
    Task<bool> DeleteThreadAsync(Guid uuid, CancellationToken ct = default);
}