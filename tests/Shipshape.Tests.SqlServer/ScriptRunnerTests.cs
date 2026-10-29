using Microsoft.EntityFrameworkCore;
using Shipshape.Tests.Toys;

namespace Shipshape.Tests.SqlServer;

public class ScriptRunnerTests
{
    [Fact]
    public async Task RunAsync_ShouldCreateAndMigrateDatabase_WhenHeaderedScriptRunsTwiceFromServer()
    {
        var server = SqlServer.ConnectionString();
        await using var database = ThrowawayDatabase.Reserve(server);
        await using var context = ToyContexts.Create("Library");
        var header = ScriptHeader.QuotedIdentifierOn | ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase;
        var script = ScriptHeaderWriter.Write(header, database.Name) + ScriptGenerator.Generate(context);

        await ScriptRunner.RunAsync(server, script);
        await ScriptRunner.RunAsync(server, script);

        context.Database.SetConnectionString(database.ConnectionString);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
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
}
