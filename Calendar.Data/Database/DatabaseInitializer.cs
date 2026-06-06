using Microsoft.Data.Sqlite;

namespace Calendar.Data.Database;

public static class DatabaseInitializer
{
    public static string GetDatabasePath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CalendarWidget");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "calendar.db");
    }

    public static void Initialize(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Tasks (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT,
                DueDate TEXT,
                Priority INTEGER NOT NULL DEFAULT 1,
                CompletionPercent INTEGER NOT NULL DEFAULT 0,
                IsCompleted INTEGER NOT NULL DEFAULT 0,
                Tags TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                SyncRevision INTEGER,
                LastSyncedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS Reminders (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                TriggerAt TEXT NOT NULL,
                LinkedTaskId TEXT,
                IsDone INTEGER NOT NULL DEFAULT 0,
                SnoozeUntil TEXT,
                Notified INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                SyncRevision INTEGER,
                LastSyncedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Json TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Notes (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                SyncRevision INTEGER,
                LastSyncedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS Attachments (
                Id TEXT PRIMARY KEY,
                OwnerId TEXT NOT NULL,
                OwnerType INTEGER NOT NULL,
                FileName TEXT NOT NULL,
                RelativePath TEXT NOT NULL,
                ContentType TEXT,
                SizeBytes INTEGER NOT NULL DEFAULT 0,
                IsImage INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                SyncRevision INTEGER,
                LastSyncedAt TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_Attachments_Owner ON Attachments(OwnerId);
            """;
        cmd.ExecuteNonQuery();
    }
}
