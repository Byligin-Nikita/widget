using Calendar.Core.Models;
using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Microsoft.Data.Sqlite;

namespace Calendar.Data.Repositories;

public sealed class NoteRepository : INoteRepository
{
    private readonly SqliteConnectionFactory _factory;

    public NoteRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Note>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Notes WHERE IsDeleted = 0 ORDER BY UpdatedAt DESC";
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Notes WHERE Id = $id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task SaveAsync(Note item, CancellationToken ct = default)
    {
        item.Touch();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Notes
            (Id, Title, Content, CreatedAt, UpdatedAt, IsDeleted, SyncRevision, LastSyncedAt)
            VALUES ($id, $title, $content, $created, $updated, $deleted, $rev, $synced)
            """;
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$title", item.Title);
        cmd.Parameters.AddWithValue("$content", item.Content);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", item.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$deleted", item.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$rev", (object?)item.SyncRevision ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$synced", item.LastSyncedAt.HasValue ? item.LastSyncedAt.Value.ToString("O") : DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(id, ct);
        if (item is null) return;
        item.IsDeleted = true;
        await SaveAsync(item, ct);
    }

    private static async Task<IReadOnlyList<Note>> ReadAllAsync(SqliteCommand cmd, CancellationToken ct)
    {
        var list = new List<Note>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    private static Note Map(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        Title = r.GetString(1),
        Content = r.GetString(2),
        CreatedAt = DateTime.Parse(r.GetString(3)),
        UpdatedAt = DateTime.Parse(r.GetString(4)),
        IsDeleted = r.GetInt32(5) == 1,
        SyncRevision = r.IsDBNull(6) ? null : r.GetInt64(6),
        LastSyncedAt = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7))
    };
}
