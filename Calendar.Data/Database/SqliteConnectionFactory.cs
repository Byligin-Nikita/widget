using Microsoft.Data.Sqlite;

namespace Calendar.Data.Database;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory()
    {
        var path = DatabaseInitializer.GetDatabasePath();
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        DatabaseInitializer.Initialize(_connectionString);
    }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
