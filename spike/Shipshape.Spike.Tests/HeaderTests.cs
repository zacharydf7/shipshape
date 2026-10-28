using Microsoft.EntityFrameworkCore;
using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

// Q2: the header is generated from flags, so a script never carries a hand-written one.
public class HeaderTests
{
    private const ScriptHeader Full = ScriptHeader.QuotedIdentifierOn | ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase;

    [Theory]
    [InlineData(Full, "Library", "Full.sql")]
    [InlineData(ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase, "Store", "CreateAndUse.sql")]
    [InlineData(ScriptHeader.UseDatabase, "Bob's ]Shop", "UseEscaped.sql")]
    public void Write_ShouldMatchGoldenFile_WhenGivenFlags(ScriptHeader header, string databaseName, string goldenFile)
    {
        var golden = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Headers", goldenFile)).ReplaceLineEndings("\n");

        var text = ScriptHeaderWriter.Write(header, databaseName);

        Assert.Equal(golden, text);
    }

    [Fact]
    public async Task RunAsync_ShouldCreateAndMigrateDatabase_WhenHeaderedScriptRunsTwiceFromServer()
    {
        var server = SqlServer.ConnectionString();
        await using var database = ThrowawayDatabase.Reserve(server);
        await using var context = ToyContexts.Create("Library");
        var script = ScriptHeaderWriter.Write(Full, database.Name) + ScriptGenerator.Generate(context);

        await ScriptRunner.RunAsync(server, script);
        await ScriptRunner.RunAsync(server, script);

        context.Database.SetConnectionString(database.ConnectionString);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }
}
