using Calendar.Core.Models;
using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Microsoft.Data.Sqlite;

namespace Calendar.Data.Repositories;

public sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly SqliteConnectionFactory _factory;

    public AttachmentRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<Attachment>> GetForOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Attachments WHERE IsDeleted = 0 AND OwnerId = $owner ORDER BY CreatedAt";
        cmd.Parameters.AddWithValue("$owner", ownerId.ToString());
        return await ReadAllAsync(cmd, ct);
    }

    public async Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Attachments WHERE Id = $id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task SaveAsync(Attachment item, CancellationToken ct = default)
    {
        item.Touch();
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Attachments
            (Id, OwnerId, OwnerType, FileName, RelativePath, ContentType, SizeBytes, IsImage,
             CreatedAt, UpdatedAt, IsDeleted, SyncRevision, LastSyncedAt)
            VALUES ($id, $owner, $otype, $name, $path, $ctype, $size, $img,
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

    private static async Task<IReadOnlyList<Attachment>> ReadAllAsync(SqliteCommand cmd, CancellationToken ct)
    {
        var list = new List<Attachment>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(Map(reader));
        return list;
    }

    private static void Bind(SqliteCommand cmd, Attachment item)
    {
        cmd.Parameters.AddWithValue("$id", item.Id.ToString());
        cmd.Parameters.AddWithValue("$owner", item.OwnerId.ToString());
        cmd.Parameters.AddWithValue("$otype", (int)item.OwnerType);
        cmd.Parameters.AddWithValue("$name", item.FileName);
        cmd.Parameters.AddWithValue("$path", item.RelativePath);
        cmd.Parameters.AddWithValue("$ctype", (object?)item.ContentType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$size", item.SizeBytes);
        cmd.Parameters.AddWithValue("$img", item.IsImage ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$updated", item.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$deleted", item.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$rev", (object?)item.SyncRevision ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$synced", item.LastSyncedAt.HasValue ? item.LastSyncedAt.Value.ToString("O") : DBNull.Value);
    }

    private static Attachment Map(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        OwnerId = Guid.Parse(r.GetString(1)),
        OwnerType = (AttachmentOwnerType)r.GetInt32(2),
        FileName = r.GetString(3),
        RelativePath = r.GetString(4),
        ContentType = r.IsDBNull(5) ? null : r.GetString(5),
        SizeBytes = r.GetInt64(6),
        IsImage = r.GetInt32(7) == 1,
        CreatedAt = DateTime.Parse(r.GetString(8)),
        UpdatedAt = DateTime.Parse(r.GetString(9)),
        IsDeleted = r.GetInt32(10) == 1,
        SyncRevision = r.IsDBNull(11) ? null : r.GetInt64(11),
        LastSyncedAt = r.IsDBNull(12) ? null : DateTime.Parse(r.GetString(12))
    };
}
