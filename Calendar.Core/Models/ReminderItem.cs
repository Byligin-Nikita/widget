namespace Calendar.Core.Models;

public sealed class ReminderItem : SyncEntityBase
{
    public string Title { get; set; } = string.Empty;
    public DateTime TriggerAt { get; set; }
    public Guid? LinkedTaskId { get; set; }
    public bool IsDone { get; set; }
    public DateTime? SnoozeUntil { get; set; }
    public bool Notified { get; set; }

    public DateTime EffectiveTriggerAt => SnoozeUntil ?? TriggerAt;
}
