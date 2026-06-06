using System.Text.Json;
using Calendar.Core.Models;
using Calendar.Core.Repositories;
using Calendar.Data.Database;
using Microsoft.Data.Sqlite;

namespace Calendar.Data.Repositories;

public sealed class SettingsRepository : ISettingsRepository
{
    private readonly SqliteConnectionFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public SettingsRepository(SqliteConnectionFactory factory) => _factory = factory;

    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Json FROM AppSettings WHERE Id = 1";
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is string json && !string.IsNullOrWhiteSpace(json))
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        return new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO AppSettings (Id, Json) VALUES (1, $json)
            """;
        cmd.Parameters.AddWithValue("$json", JsonSerializer.Serialize(settings, JsonOptions));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
