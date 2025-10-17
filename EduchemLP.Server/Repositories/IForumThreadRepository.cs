using EduchemLP.Server.Classes.Objects;

namespace EduchemLP.Server.Repositories;

public interface IForumThreadRepository
{
    Task<IEnumerable<ForumThread>> GetThreadsAsync(int requesterId, string accountType);
    Task<ForumThread?> GetThreadByUuidAsync(Guid uuid, int requesterId, string accountType);
    Task<bool> CreateThreadAsync(ForumThread thread);
    Task<bool> ApproveThreadAsync(Guid uuid, int approverId, string accountType);
    Task<bool> DeleteThreadAsync(Guid uuid, int requesterId, string accountType);
}