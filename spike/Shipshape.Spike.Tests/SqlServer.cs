using Microsoft.EntityFrameworkCore;
using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

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

    /// <summary>The script the database tests apply. Planner's custom SQL only runs with EXEC wrapping on.</summary>
    public static string ScriptFor(DbContext context)
    {
        if (context is not PlannerContext)
        {
            return ScriptGenerator.Generate(context);
        }

        using var wrapped = ScriptGenerator.WithExecWrapping(context);
        return ScriptGenerator.Generate(wrapped);
    }
}
