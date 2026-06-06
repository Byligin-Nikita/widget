using Calendar.Core.Models;

namespace Calendar.Core.Repositories;

public interface IAttachmentRepository
{
    Task<IReadOnlyList<Attachment>> GetForOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(Attachment item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
