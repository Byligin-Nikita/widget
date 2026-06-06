namespace Calendar.Core.Models;

public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

public sealed class TaskItem : SyncEntityBase
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public int CompletionPercent { get; set; }
    public bool IsCompleted { get; set; }
    public string? Tags { get; set; }
}
