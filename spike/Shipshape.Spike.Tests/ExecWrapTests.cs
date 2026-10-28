using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

// Q3: custom SQL that needs EXEC is detected with a message naming the fix, and both fixes produce a passing script.
public class ExecWrapTests
{
    [Fact]
    public async Task Explain_ShouldNameMigrationAndFix_WhenCustomSqlUsesColumnAddedInSameMigration()
    {
        var server = SqlServer.ConnectionString();
        await using var database = await ThrowawayDatabase.CreateAsync(server);
        await using var context = ToyContexts.Create("Planner");

        var failure = await Assert.ThrowsAsync<ScriptBatchException>(
            () => ScriptRunner.RunAsync(database.ConnectionString, ScriptGenerator.Generate(context)));

        Assert.Equal(207, failure.ErrorNumber);
        Assert.Equal("20260126090200_AddTaskPriority", failure.MigrationId);
        Assert.Contains("Migration 20260126090200_AddTaskPriority fails", SqlErrorExplainer.Explain(context, failure));
    }

    [Fact]
    public async Task Explain_ShouldNameMigrationAndFix_WhenCustomSqlCreatesView()
    {
        var server = SqlServer.ConnectionString();
        await using var database = await ThrowawayDatabase.CreateAsync(server);
        await using var context = ToyContexts.Create("Planner");
        var viewOnly = ScriptGenerator.Generate(context, "20260126090200_AddTaskPriority", "20260126090300_CreateOpenTasksView");

        var failure = await Assert.ThrowsAsync<ScriptBatchException>(() => ScriptRunner.RunAsync(database.ConnectionString, viewOnly));

        Assert.Equal("20260126090300_CreateOpenTasksView", failure.MigrationId);
        Assert.Contains("CREATE VIEW, PROCEDURE, FUNCTION and TRIGGER", SqlErrorExplainer.Explain(context, failure));
    }

    [Fact]
    public async Task Explain_ShouldReturnNull_WhenFailureIsNotAboutCustomSql()
    {
        var server = SqlServer.ConnectionString();
        await using var database = await ThrowawayDatabase.CreateAsync(server);
        await using var context = ToyContexts.Create("Library");
        await ScriptRunner.RunAsync(database.ConnectionString, "CREATE TABLE [Authors] ([Id] int NOT NULL);");

        var failure = await Assert.ThrowsAsync<ScriptBatchException>(
            () => ScriptRunner.RunAsync(database.ConnectionString, ScriptGenerator.Generate(context)));

        Assert.Null(SqlErrorExplainer.Explain(context, failure));
    }

    [Fact]
    public async Task RunAsync_ShouldFindNoProblems_WhenGeneratorWrapsCustomSql()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create("Planner");
        using var wrapped = ScriptGenerator.WithExecWrapping(context);

        var problems = await ParityCheck.RunAsync(context, ScriptGenerator.Generate(wrapped), server);

        Assert.Empty(problems);
    }

    [Fact]
    public async Task RunAsync_ShouldFindNoProblems_WhenMigrationsUseSqlExecHelper()
    {
        var server = SqlServer.ConnectionString();
        await using var context = ToyContexts.Create("Checklist");

        var problems = await ParityCheck.RunAsync(context, ScriptGenerator.Generate(context), server);

        Assert.Empty(problems);
    }

    // Library and Billing wrap their custom SQL by hand; Billing also has its own history table, which must survive.
    [Theory]
    [InlineData("Library")]
    [InlineData("Billing")]
    public void WithExecWrapping_ShouldNotChangeScript_WhenCustomSqlIsAlreadyWrapped(string name)
    {
        using var context = ToyContexts.Create(name);
        using var wrapped = ScriptGenerator.WithExecWrapping(context);

        var script = ScriptGenerator.Generate(wrapped);

        Assert.Equal(ScriptGenerator.Generate(context), script);
    }
}
