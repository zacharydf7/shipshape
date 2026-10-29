using Shipshape.Tests.Toys;

namespace Shipshape.Tests.Unit;

public class MigrationScriptTests
{
    // The toy scripts under tests/Scripts are checked in like a consumer's, so this is the file test a consumer writes.
    [Theory]
    [InlineData("Library")]
    [InlineData("Catalog")]
    [InlineData("Billing")]
    [InlineData("Planner")]
    public async Task VerifyFileAsync_ShouldPass_WhenToyScriptIsCurrent(string name)
    {
        await using var context = ToyContexts.Create(name);

        var script = MigrationScript.For(context, ToyContexts.ScriptPath(name), ToyContexts.ScriptOptions(name));

        await script.VerifyFileAsync();
    }

    [Fact]
    public void For_ShouldThrow_WhenHeaderUsesDatabaseWithoutName()
    {
        using var context = ToyContexts.Create("Library");
        var options = new MigrationScriptOptions { Header = ScriptHeader.UseDatabase };

        var error = Assert.Throws<ArgumentException>(() => MigrationScript.For(context, ToyContexts.ScriptPath("Library"), options));

        Assert.Contains(nameof(MigrationScriptOptions.DatabaseName), error.Message);
    }

    [Fact]
    public void For_ShouldFindScriptAboveOutputFolder_WhenGivenRelativePath()
    {
        using var context = ToyContexts.Create("Library");

        var script = MigrationScript.For(context, ToyContexts.ScriptPath("Library"));

        Assert.True(File.Exists(script.FullPath));
        Assert.EndsWith(Path.Combine("tests", "Scripts", "Library.sql"), script.FullPath);
    }
}
