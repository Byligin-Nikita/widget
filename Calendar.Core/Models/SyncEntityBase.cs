namespace Calendar.Core.Models;

public abstract class SyncEntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public long? SyncRevision { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
