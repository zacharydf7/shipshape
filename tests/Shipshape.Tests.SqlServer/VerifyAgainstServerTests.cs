using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Shipshape.Tests.Toys;

namespace Shipshape.Tests.SqlServer;

public class VerifyAgainstServerTests
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

    // The checked-in toy scripts under tests/Scripts, as a consumer's second test uses them.
    [Theory]
    [InlineData("Library")]
    [InlineData("Catalog")]
    [InlineData("Billing")]
    [InlineData("Planner")]
    public async Task VerifyAgainstServerAsync_ShouldPass_WhenToyScriptIsCurrent(string name)
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create(name);

        var script = MigrationScript.For(context, ToyContexts.ScriptPath(name), ToyContexts.ScriptOptions(name));

        await script.VerifyAgainstServerAsync(server);
    }

    [Theory]
    [MemberData(nameof(Breaks))]
    public async Task VerifyAgainstServerAsync_ShouldReportCheck_WhenScriptIsBroken(string name, string breakName, ScriptCheck expected)
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create(name);
        using var folder = new TempFolder();
        var path = folder.File(name + ".sql");
        var original = MigrationScript.For(context, ToyContexts.ScriptPath(name)).FullPath;
        await File.WriteAllTextAsync(path, Break(await File.ReadAllTextAsync(original, TestContext.Current.CancellationToken), breakName),
            TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => MigrationScript.For(context, path, ToyContexts.ScriptOptions(name)).VerifyAgainstServerAsync(server));

        Assert.Contains($"{expected}: ", error.Message);
    }

    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldNameMigrationAndFix_WhenCustomSqlNeedsExec()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.UnwrappedPlanner();
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Planner.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => MigrationScript.For(context, folder.File("Planner.sql")).VerifyAgainstServerAsync(server));

        Assert.Contains("AppliesToEmptyDatabase: Migration 20260126090200_AddTaskPriority fails in the idempotent script", error.Message);
        Assert.Contains("WrapCustomSqlInExec()", error.Message);
    }

    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldStillReportFailedRun_WhenAppliesToEmptyDatabaseIsSkipped()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.UnwrappedPlanner();
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Planner.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);
        var options = new MigrationScriptOptions { Skip = { ScriptCheck.AppliesToEmptyDatabase } };

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => MigrationScript.For(context, folder.File("Planner.sql"), options).VerifyAgainstServerAsync(server));

        Assert.Contains("AppliesToEmptyDatabase: ", error.Message);
    }

    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldPass_WhenContextSeedsRowsInMigrate()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.LibraryWithSeeding();

        var script = MigrationScript.For(context, ToyContexts.ScriptPath("Library"), ToyContexts.ScriptOptions("Library"));

        await script.VerifyAgainstServerAsync(server);
    }

    // Checklist fixes the same custom SQL as Planner with migrationBuilder.SqlExec instead of WrapCustomSqlInExec.
    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldPass_WhenMigrationsUseSqlExec()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create("Checklist");
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Checklist.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);

        await MigrationScript.For(context, folder.File("Checklist.sql")).VerifyAgainstServerAsync(server);
    }

    // EF Core's defaults: Migrate() refuses to run while there are pending model changes.
    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldReportPendingModelChanges_WhenModelHasNoMigration()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create("Drift");
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Drift.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => MigrationScript.For(context, folder.File("Drift.sql")).VerifyAgainstServerAsync(server));

        Assert.Contains("NoPendingModelChanges: The model has changes that no migration covers", error.Message);
        Assert.Contains("The other checks need Migrate(), which refuses to run", error.Message);
    }

    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldNameWarningToIgnore_WhenSkippingPendingModelChangesWithDefaults()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create("Drift");
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Drift.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);
        var options = new MigrationScriptOptions { Skip = { ScriptCheck.NoPendingModelChanges } };

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => MigrationScript.For(context, folder.File("Drift.sql"), options).VerifyAgainstServerAsync(server));

        Assert.Contains("Ignore(RelationalEventId.PendingModelChangesWarning)", error.Message);
    }

    [Fact]
    public async Task VerifyAgainstServerAsync_ShouldPass_WhenPendingModelChangesAreSkippedAndWarningIgnored()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.DriftIgnoringPendingModelChanges();
        using var folder = new TempFolder();
        await File.WriteAllTextAsync(folder.File("Drift.sql"), ScriptGenerator.Generate(context), TestContext.Current.CancellationToken);
        var options = new MigrationScriptOptions { Skip = { ScriptCheck.NoPendingModelChanges } };
        var connectionString = context.Database.GetConnectionString();

        await MigrationScript.For(context, folder.File("Drift.sql"), options).VerifyAgainstServerAsync(server);

        Assert.Equal(connectionString, context.Database.GetConnectionString());
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
