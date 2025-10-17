using System.Text.Json.Serialization;

namespace EduchemLP.Server.Classes.Objects;

public class ForumThread
{
    
    public Guid Uuid { get; private set; }
    
    public string Title { get; private set; }
    
    public string Text { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public ForumThreadAuthor Author { get; private set; }
    
    public bool IsPinned { get; private set; }
    
    public bool IsApproved { get; private set; }
    
    [JsonConstructor]
    
    public ForumThread(Guid uuid, string title, string text, DateTime createdAt, ForumThreadAuthor author,  bool isPinned, bool isApproved)
    {
        Uuid = uuid;
        Title = title;
        Text = text;
        CreatedAt = createdAt;
        Author = author;
        IsPinned = isPinned;
        IsApproved = isApproved;
    }
}
public class ForumThreadAuthor
{
    public int Id { get; private set; }
        
    public string DisplayName { get; private set; }
        
    public string? Avatar { get; private set; }
        
    public ForumThreadAuthor(int id, string displayName, string? avatar)
    {
        Id = id;
        DisplayName = displayName;
        Avatar = avatar;
    }
}

