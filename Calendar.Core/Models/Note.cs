namespace Calendar.Core.Models;

public sealed class Note : SyncEntityBase
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
