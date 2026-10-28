using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shipshape.Spike.Tests.Toys;

namespace Shipshape.Spike.Tests;

// Q1: a script can be generated in-process, with no database, and matches `dotnet ef migrations script --idempotent`.
public class GenerateTests
{
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
