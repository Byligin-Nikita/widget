using Calendar.Core.Models;

namespace Calendar.Core.Repositories;

public interface INoteRepository
{
    Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default);
    Task<Note?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Note item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
