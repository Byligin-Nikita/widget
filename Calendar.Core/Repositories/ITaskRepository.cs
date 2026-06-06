using Calendar.Core.Models;

namespace Calendar.Core.Repositories;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByDueDateAsync(DateTime date, CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(TaskItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
