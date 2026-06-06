using Calendar.Core.Models;

namespace Calendar.Core.Repositories;

public interface ISettingsRepository
{
    Task<AppSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
