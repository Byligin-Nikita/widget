using Calendar.Core.Models;

namespace Calendar.Core.Repositories;

public interface IReminderRepository
{
    Task<IReadOnlyList<ReminderItem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReminderItem>> GetPendingAsync(CancellationToken ct = default);
    Task<ReminderItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(ReminderItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
