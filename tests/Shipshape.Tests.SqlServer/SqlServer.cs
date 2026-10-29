namespace Shipshape.Tests.SqlServer;

public static class SqlServer
{
    public const string Variable = "SHIPSHAPE_SQLSERVER";

    /// <summary>The server to test against, without a database. Skips the calling test when none is configured.</summary>
    public static string ConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrEmpty(connectionString))
        {
            Assert.Skip($"Set {Variable} to a SQL Server connection string to run this test.");
        }

        return connectionString;
    }
}
