namespace Shipshape.Tests.Unit;

public class ScriptDiffTests
{
    [Fact]
    public void Describe_ShouldShowOnlyAddedLines_WhenMigrationIsAppended()
    {
        var diff = ScriptDiff.Describe("A\nB\nEND\n", "A\nB\nC\nD\nEND\n");

        Assert.Equal("@@ line 3 @@\n+ C\n+ D\n", diff);
    }

    [Fact]
    public void Describe_ShouldCutLongSides_WhenMoreThanFortyLinesChange()
    {
        var added = string.Concat(Enumerable.Range(1, 45).Select(i => $"L{i}\n"));

        var diff = ScriptDiff.Describe("", added);

        Assert.EndsWith("+ L40\n+ ... 5 more lines\n", diff);
    }

    [Fact]
    public void EfVersionChange_ShouldReturnNull_WhenVersionsMatch()
    {
        const string script = "VALUES (N'1_First', N'10.0.14');\n";

        var change = ScriptDiff.EfVersionChange(script, script);

        Assert.Null(change);
    }
}
