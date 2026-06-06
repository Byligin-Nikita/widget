namespace Calendar.Core.Sync;

public sealed class NoOpCloudSyncProvider : ICloudSyncProvider
{
    public bool IsConfigured => false;

    public Task SyncAsync(CancellationToken ct = default) => Task.CompletedTask;
}
