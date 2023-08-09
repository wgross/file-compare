using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace FileCompare.Persistence;

public sealed class InMemoryDbContextOptionsBuilder : IDisposable
{
    public static InMemoryDbContextOptionsBuilder Default { get; } = new();

    private DbConnection connection;

    public DbContextOptions CreateOptions(DbContextOptionsBuilder opts)
    {
        if (this.connection is null)
            this.InitializeDbConnection();

        return opts.UseSqlite(this.connection).Options;
    }

    private void InitializeDbConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder();
        connectionString.DataSource = ":memory:";

        this.connection = new SqliteConnection(connectionString.ConnectionString);
        this.connection.Open();
    }

    #region IDisposable

    public void Dispose()
    {
        this.connection?.Dispose();
        this.connection = null;
    }

    #endregion IDisposable
}