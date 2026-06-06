using Calendar.Core.Models;
using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Microsoft.Data.Sqlite;

namespace Calendar.Data.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TaskRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Tasks WHERE IsDeleted = 0 ORDER BY DueDate, Title";
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByDueDateAsync(DateTime date, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM Tasks
            WHERE IsDeleted = 0 AND DueDate IS NOT NULL
            AND date(DueDate) = date($date)
            ORDER BY Title
            """;
        cmd.Parameters.AddWithValue("$date", date.ToString("O"));
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Tasks WHERE Id = $id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task SaveAsync(TaskItem item, CancellationToken ct = default)
    {
        item.Touch();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Tasks
            (Id, Title, Description, DueDate, Priority, CompletionPercent, IsCompleted, Tags,
             CreatedAt, UpdatedAt, IsDeleted, SyncRevision, LastSyncedAt)
            VALUES ($id, $title, $desc, $due, $prio, $pct, $done, $tags,
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

    private static async Task<IReadOnlyList<TaskItem>> ReadAllAsync(SqliteCommand cmd, CancellationToken ct)
    {
        var list = new List<TaskItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    private static void Bind(SqliteCommand cmd, TaskItem item)
    {
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$desc", (object?)item.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$due", item.DueDate.HasValue ? item.DueDate.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("$prio", (int)item.Priority);
        cmd.Parameters.AddWithValue("$pct", item.CompletionPercent);
        cmd.Parameters.AddWithValue("$done", item.IsCompleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$tags", (object?)item.Tags ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", item.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$deleted", item.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$rev", (object?)item.SyncRevision ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$synced", item.LastSyncedAt.HasValue ? item.LastSyncedAt.Value.ToString("O") : DBNull.Value);
    }

    private static TaskItem Map(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        Title = r.GetString(1),
        Description = r.IsDBNull(2) ? null : r.GetString(2),
        DueDate = r.IsDBNull(3) ? null : DateTime.Parse(r.GetString(3)),
        Priority = (TaskPriority)r.GetInt32(4),
        CompletionPercent = r.GetInt32(5),
        IsCompleted = r.GetInt32(6) == 1,
        Tags = r.IsDBNull(7) ? null : r.GetString(7),
        CreatedAt = DateTime.Parse(r.GetString(8)),
        UpdatedAt = DateTime.Parse(r.GetString(9)),
        IsDeleted = r.GetInt32(10) == 1,
        SyncRevision = r.IsDBNull(11) ? null : r.GetInt64(11),
        LastSyncedAt = r.IsDBNull(12) ? null : DateTime.Parse(r.GetString(12))
    };
}
