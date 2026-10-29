using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shipshape.Tests.Toys;

namespace Shipshape.Tests.Unit;

public class ScriptGeneratorTests
{
    // The goldens are `dotnet ef migrations script --idempotent` output for the same contexts, Planner's wrapping included.
    [Theory]
    [InlineData("Library")]
    [InlineData("Catalog")]
    [InlineData("Billing")]
    [InlineData("Planner")]
    public void Generate_ShouldMatchDotnetEfWithoutConnecting_WhenGivenToyContext(string name)
    {
        var connections = new ConnectionCounter();
        using var context = ToyContexts.Create(name, connections);
        var dotnetEf = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DotnetEf", name + ".sql")).ReplaceLineEndings("\n");

        var script = ScriptGenerator.Generate(context);

        Assert.Equal(dotnetEf, script);
        Assert.Equal(0, connections.Opened);
    }

    // Library and Billing wrap their custom SQL by hand, Checklist through SqlExec; Billing also has its own history
    // table, which must survive.
    [Theory]
    [InlineData("Library")]
    [InlineData("Billing")]
    [InlineData("Checklist")]
    public void Generate_ShouldLeaveScriptUnchanged_WhenCustomSqlIsAlreadyWrapped(string name)
    {
        using var plain = ToyContexts.Create(name);
        using var wrapping = ToyContexts.Create(name, wrapCustomSql: true);

        var script = ScriptGenerator.Generate(wrapping);

        Assert.Equal(ScriptGenerator.Generate(plain), script);
    }

    private sealed class ConnectionCounter : DbConnectionInterceptor
    {
        public int Opened { get; private set; }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Opened++;
            return result;
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            Opened++;
            return ValueTask.FromResult(result);
        }
    }
}
