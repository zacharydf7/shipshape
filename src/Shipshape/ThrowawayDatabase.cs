using Microsoft.Data.SqlClient;

namespace Shipshape;

/// <summary>A uniquely named database that is dropped on dispose.</summary>
internal sealed class ThrowawayDatabase : IAsyncDisposable
{
    private const string Prefix = "shipshape_";

    private readonly string _serverConnectionString;

    private ThrowawayDatabase(string serverConnectionString)
    {
        _serverConnectionString = serverConnectionString;
        Name = Prefix + Guid.NewGuid().ToString("N");
        ConnectionString = new SqlConnectionStringBuilder(serverConnectionString) { InitialCatalog = Name }.ConnectionString;
    }

    public string Name { get; }

    public string ConnectionString { get; }

    /// <summary>Picks a name without creating the database, for a script that creates it itself.</summary>
    public static ThrowawayDatabase Reserve(string serverConnectionString) => new(serverConnectionString);

    public static async Task<ThrowawayDatabase> CreateAsync(string serverConnectionString)
    {
        var database = new ThrowawayDatabase(serverConnectionString);
        await database.ExecuteOnServerAsync($"CREATE DATABASE [{database.Name}]");
        return database;
    }

    public async ValueTask DisposeAsync()
    {
        // Pooled connections would otherwise keep the database in use.
        await using (var pooled = new SqlConnection(ConnectionString))
        {
            SqlConnection.ClearPool(pooled);
        }

        await ExecuteOnServerAsync($"""
            IF DB_ID(N'{Name}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{Name}];
            END
            """);
    }

    private async Task ExecuteOnServerAsync(string sql)
    {
        await using var connection = new SqlConnection(_serverConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
