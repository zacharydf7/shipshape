namespace Shipshape.Tests.Unit;

public class ScriptHeaderWriterTests
{
    [Fact]
    public void Write_ShouldWriteEveryStatementInOrder_WhenGivenAllFlags()
    {
        var header = ScriptHeader.UseDatabase | ScriptHeader.QuotedIdentifierOn | ScriptHeader.CreateDatabaseIfMissing;

        var text = ScriptHeaderWriter.Write(header, "Library");

        Assert.Equal("""
            SET QUOTED_IDENTIFIER ON;
            GO

            IF DB_ID(N'Library') IS NULL
                CREATE DATABASE [Library];
            GO

            USE [Library];
            GO


            """.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public void Write_ShouldEscapeName_WhenNameHasQuoteAndBracket()
    {
        var text = ScriptHeaderWriter.Write(ScriptHeader.CreateDatabaseIfMissing, "Bob's ]Shop");

        Assert.Equal("IF DB_ID(N'Bob''s ]Shop') IS NULL\n    CREATE DATABASE [Bob's ]]Shop];\nGO\n\n", text);
    }

    [Fact]
    public void Write_ShouldWriteNothing_WhenGivenNone()
    {
        var text = ScriptHeaderWriter.Write(ScriptHeader.None, "Library");

        Assert.Equal("", text);
    }
}
