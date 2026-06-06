namespace Calendar.Core.Sync;

public interface ICloudSyncProvider
{
    bool IsConfigured { get; }
    Task SyncAsync(CancellationToken ct = default);
}
