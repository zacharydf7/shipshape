using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

// Q4: the parity check finds no differences in correct scripts, run after run, and catches each deliberate break.
public class SchemaTests
{
    public static TheoryData<string, string, ScriptCheck> Breaks => new()
    {
        { "Library", "drop a filter from an index", ScriptCheck.MatchesMigratedSchema },
        { "Library", "remove a column", ScriptCheck.MatchesMigratedSchema },
        { "Catalog", "delete a history insert", ScriptCheck.RecordsEveryMigration },
        { "Library", "un-wrap the EXEC on a backfill", ScriptCheck.AppliesToEmptyDatabase },
        { "Billing", "add a non-idempotent INSERT", ScriptCheck.ChangesNothingOnRerun },
        { "Billing", "change a foreign key to cascade", ScriptCheck.MatchesMigratedSchema },
    };

    [Theory]
    [InlineData("Library")]
    [InlineData("Catalog")]
    [InlineData("Billing")]
    [InlineData("Planner")]
    public async Task RunAsync_ShouldFindNoProblems_WhenRunFiveTimes(string name)
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create(name);
        var script = SqlServer.ScriptFor(context);

        for (var run = 1; run <= 5; run++)
        {
            var problems = await ParityCheck.RunAsync(context, script, server);

            Assert.Empty(problems);
        }
    }

    [Theory]
    [MemberData(nameof(Breaks))]
    public async Task RunAsync_ShouldReportBreak_WhenScriptIsBroken(string name, string breakName, ScriptCheck expected)
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create(name);
        var script = Break(SqlServer.ScriptFor(context), breakName);

        var problems = await ParityCheck.RunAsync(context, script, server);

        Assert.Contains(problems, problem => problem.Check == expected);
    }

    [Fact]
    public async Task RunAsync_ShouldRecordBothHistories_WhenTwoContextsShareDatabase()
    {
        var server = SqlServer.ConnectionString();
        await using var database = await ThrowawayDatabase.CreateAsync(server);
        await using var catalog = ToyContexts.Create("Catalog");
        await using var billing = ToyContexts.Create("Billing");

        foreach (var context in new[] { catalog, billing, catalog, billing })
        {
            await ScriptRunner.RunAsync(database.ConnectionString, ScriptGenerator.Generate(context));
        }

        catalog.Database.SetConnectionString(database.ConnectionString);
        billing.Database.SetConnectionString(database.ConnectionString);
        Assert.Empty(await catalog.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await billing.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    private static string Break(string script, string breakName) => breakName switch
    {
        "drop a filter from an index" => ReplaceOnce(script, " WHERE [Isbn] IS NOT NULL')", "')"),
        "remove a column" => ReplaceOnce(script, "        [LoanedOn] datetime2 NOT NULL,\n", ""),
        "delete a history insert" => RemoveOnce(script, HistoryInsert("20260112090800_AddProductNameIndex")),
        "un-wrap the EXEC on a backfill" => ReplaceOnce(
            script, "EXEC(N'UPDATE [Members] SET [DisplayName] = LEFT([Email], 100) WHERE LEN([DisplayName]) = 0')",
            "UPDATE [Members] SET [DisplayName] = LEFT([Email], 100) WHERE LEN([DisplayName]) = 0"),
        "add a non-idempotent INSERT" => script
            + "INSERT INTO [billing].[Customers] ([Name], [Email], [CountryCode]) VALUES (N'Walk-in', N'walk-in@example.com', 'US');\nGO\n",
        "change a foreign key to cascade" => ReplaceOnce(
            script, "REFERENCES [billing].[Customers] ([Id]) ON DELETE NO ACTION",
            "REFERENCES [billing].[Customers] ([Id]) ON DELETE CASCADE"),
        _ => throw new ArgumentOutOfRangeException(nameof(breakName), breakName, "Not a known break."),
    };

    // A break that matched nothing would let the test pass without breaking anything.
    private static string ReplaceOnce(string script, string oldText, string newText)
    {
        Assert.Single(Regex.Matches(script, Regex.Escape(oldText)));
        return script.Replace(oldText, newText);
    }

    private static string RemoveOnce(string script, Regex pattern)
    {
        Assert.Single(pattern.Matches(script));
        return pattern.Replace(script, "");
    }

    private static Regex HistoryInsert(string migrationId) => new(
        @"IF NOT EXISTS \(\s*SELECT \* FROM \[__EFMigrationsHistory\]\s*WHERE \[MigrationId\] = N'" + migrationId
        + @"'\s*\)\s*BEGIN\s*INSERT INTO \[__EFMigrationsHistory\][^;]*;\s*END;\n");
}
