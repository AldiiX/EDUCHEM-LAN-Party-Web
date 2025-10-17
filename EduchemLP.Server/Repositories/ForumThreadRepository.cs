using EduchemLP.Server.Classes.Objects;
using EduchemLP.Server.Infrastructure;
using EduchemLP.Server.Services;
using MySqlConnector;

namespace EduchemLP.Server.Repositories;

public class ForumThreadRepository(IDatabaseService db) : IForumThreadRepository
{
    public async Task<IEnumerable<ForumThread>> GetThreadsAsync(int requesterId, string accountType)
    {
        await using var conn = await db.GetOpenConnectionAsync();
        await using var cmd = conn!.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                ft.uuid,
                ft.title,
                ft.text,
                ft.created_at,
                u.id AS author_id,
                u.display_name AS author_display_name,
                u.avatar AS author_avatar,
                ft.is_pinned,
                ft.is_approved
            FROM forum_threads AS ft
            JOIN users AS u ON ft.author_id = u.id
            ORDER BY ft.is_pinned DESC, ft.created_at DESC;
        ";
        
        await using var reader = await cmd.ExecuteReaderAsync();
        var threads = new List<ForumThread>();
        while (await reader.ReadAsync())
        {
            var thread = new ForumThread(
                reader.GetGuid("uuid"),
                reader.GetString("title"),
                reader.GetString("text"),
                reader.GetDateTime("created_at"),
                new ForumThreadAuthor(
                    reader.GetInt32("author_id"),
                    reader.GetString("author_display_name"),
                    reader.GetStringOrNull("author_avatar")
                ),
                reader.GetBoolean("is_pinned"),
                reader.GetBoolean("is_approved")
            );
            threads.Add(thread);
        }
        
        return threads;
    }

    public async Task<ForumThread?> GetThreadByUuidAsync(Guid uuid, int requesterId, string accountType)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> CreateThreadAsync(ForumThread thread)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ApproveThreadAsync(Guid uuid, int approverId, string accountType)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteThreadAsync(Guid uuid, int requesterId, string accountType)
    {
        throw new NotImplementedException();
    }
}