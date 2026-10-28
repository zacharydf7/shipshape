using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Shipshape.Spike;

/// <summary>Runs a script's batches in order on one connection and stops at the first one that fails.</summary>
public static class ScriptRunner
{
    // SqlClient's 30-second default is too short for a backfill over a large table.
    private const int BatchTimeoutSeconds = 300;

    public static async Task RunAsync(string connectionString, string script)
    {
        var batches = ScriptBatches.Split(script);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        for (var i = 0; i < batches.Count; i++)
        {
            await using var command = new SqlCommand(batches[i], connection) { CommandTimeout = BatchTimeoutSeconds };
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException e)
            {
                throw new ScriptBatchException(i + 1, batches[i], e);
            }
        }
    }
}

public sealed partial class ScriptBatchException(int batchNumber, string sql, SqlException error)
    : Exception($"Batch {batchNumber} failed with SQL error {error.Number}: {error.Message}", error)
{
    public int ErrorNumber => error.Number;

    /// <summary>The migration the batch belongs to, read from EF's IF NOT EXISTS guard; null outside a migration.</summary>
    public string? MigrationId => MigrationIdPattern().Match(sql) is { Success: true } match ? match.Groups[1].Value : null;

    [GeneratedRegex(@"\[MigrationId\] = N'([^']+)'")]
    private static partial Regex MigrationIdPattern();
}
