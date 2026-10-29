using Shipshape.Tests.Toys;

namespace Shipshape.Tests.SqlServer;

public class SqlErrorExplainerTests
{
    [Fact]
    public async Task Explain_ShouldNameFirstInBatchRule_WhenCustomSqlCreatesView()
    {
        var server = SqlServer.ConnectionString();
        await using var database = await ThrowawayDatabase.CreateAsync(server);
        await using var context = ToyContexts.UnwrappedPlanner();
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
}
