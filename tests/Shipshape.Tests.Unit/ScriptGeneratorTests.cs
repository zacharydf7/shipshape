using System.Data.Common;
using Microsoft.EntityFrameworkCore;
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

    // Billing's custom history table must survive the copy; Planner's custom SQL must come out wrapped.
    [Theory]
    [InlineData("Planner")]
    [InlineData("Billing")]
    public void GenerateWithExecWrapping_ShouldMatchOptionsExtension_WhenContextHasNoWrapping(string name)
    {
        using var unwrapped = name == "Planner" ? ToyContexts.UnwrappedPlanner() : ToyContexts.Create(name);
        using var wrapping = ToyContexts.Create(name, wrapCustomSql: true);

        var script = ScriptGenerator.GenerateWithExecWrapping(unwrapped);

        Assert.Equal(ScriptGenerator.Generate(wrapping), script);
    }

    [Fact]
    public void GenerateWithExecWrapping_ShouldThrow_WhenContextHasNoOptionsConstructor()
    {
        using var context = new NoOptionsConstructorContext();

        var error = Assert.Throws<MigrationScriptException>(() => ScriptGenerator.GenerateWithExecWrapping(context));

        Assert.Contains("needs NoOptionsConstructorContext to have a constructor that takes only its options", error.Message);
    }

    private sealed class NoOptionsConstructorContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlServer(ToyContexts.Unreachable);
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
