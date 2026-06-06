using Calendar.Core.Models;
using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Microsoft.Data.Sqlite;

namespace Calendar.Data.Repositories;

public sealed class ReminderRepository : IReminderRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ReminderRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ReminderItem>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminders WHERE IsDeleted = 0 ORDER BY TriggerAt";
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<IReadOnlyList<ReminderItem>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM Reminders
            WHERE IsDeleted = 0 AND IsDone = 0
            ORDER BY TriggerAt
            """;
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<ReminderItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Reminders WHERE Id = $id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task SaveAsync(ReminderItem item, CancellationToken ct = default)
    {
        item.Touch();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Reminders
            (Id, Title, TriggerAt, LinkedTaskId, IsDone, SnoozeUntil, Notified,
             CreatedAt, UpdatedAt, IsDeleted, SyncRevision, LastSyncedAt)
            VALUES ($id, $title, $trigger, $task, $done, $snooze, $notified,
                    $created, $updated, $deleted, $rev, $synced)
            """;
        Bind(cmd, item);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(id, ct);
        if (item is null) return;
        item.IsDeleted = true;
        await SaveAsync(item, ct);
    }

    private static async Task<IReadOnlyList<ReminderItem>> ReadAllAsync(SqliteCommand cmd, CancellationToken ct)
    {
        var list = new List<ReminderItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    private static void Bind(SqliteCommand cmd, ReminderItem item)
    {
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$trigger", item.TriggerAt.ToString("O"));
        cmd.Parameters.AddWithValue("$task", item.LinkedTaskId.HasValue ? item.LinkedTaskId.Value.ToString() : DBNull.Value);
        cmd.Parameters.AddWithValue("$done", item.IsDone ? 1 : 0);
        cmd.Parameters.AddWithValue("$snooze", item.SnoozeUntil.HasValue ? item.SnoozeUntil.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$notified", item.Notified ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", item.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$deleted", item.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$rev", (object?)item.SyncRevision ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$synced", item.LastSyncedAt.HasValue ? item.LastSyncedAt.Value.ToString("O") : DBNull.Value);
    }

    private static ReminderItem Map(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        Title = r.GetString(1),
        TriggerAt = DateTime.Parse(r.GetString(2)),
        LinkedTaskId = r.IsDBNull(3) ? null : Guid.Parse(r.GetString(3)),
        IsDone = r.GetInt32(4) == 1,
        SnoozeUntil = r.IsDBNull(5) ? null : DateTime.Parse(r.GetString(5)),
        Notified = r.GetInt32(6) == 1,
        CreatedAt = DateTime.Parse(r.GetString(7)),
        UpdatedAt = DateTime.Parse(r.GetString(8)),
        IsDeleted = r.GetInt32(9) == 1,
        SyncRevision = r.IsDBNull(10) ? null : r.GetInt64(10),
        LastSyncedAt = r.IsDBNull(11) ? null : DateTime.Parse(r.GetString(11))
    };
}
