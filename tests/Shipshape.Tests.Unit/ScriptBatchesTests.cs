namespace Shipshape.Tests.Unit;

public class ScriptBatchesTests
{
    [Fact]
    public void Split_ShouldIgnoreGo_WhenInsideStringBracketsOrBlockComment()
    {
        const string script = "SELECT 'it''s\nGO\n';\nGO\nSELECT [a\nGO\n]; /* note\ngo\n*/\n  go  \n-- it's GO\nSELECT 1;\n";

        var batches = ScriptBatches.Split(script);

        Assert.Equal(["SELECT 'it''s\nGO\n';\n", "SELECT [a\nGO\n]; /* note\ngo\n*/\n", "-- it's GO\nSELECT 1;\n\n"], batches);
    }

    [Fact]
    public void Split_ShouldDropEmptyBatches_WhenGoLinesRepeat()
    {
        var batches = ScriptBatches.Split("GO\nSELECT 1;\nGO\n\nGO\n");

        Assert.Equal(["SELECT 1;\n"], batches);
    }
}
