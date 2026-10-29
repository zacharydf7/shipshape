using System.Text;
using Shipshape.Tests.Toys;

namespace Shipshape.Tests.Unit;

public class ScriptFileTests
{
    private const string Generated = "CREATE TABLE [A] ([Id] int);\nGO\n";

    [Fact]
    public async Task VerifyAsync_ShouldPass_WhenFileDiffersOnlyInLineEndingsAndBom()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");
        await File.WriteAllTextAsync(path, Generated.ReplaceLineEndings("\r\n"), new UTF8Encoding(true), TestContext.Current.CancellationToken);

        await ScriptFile.VerifyAsync(path, Generated, accept: false);
    }

    [Fact]
    public async Task VerifyAsync_ShouldThrowWithDiffAndEfVersion_WhenFileIsStale()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");
        await File.WriteAllTextAsync(path, "SELECT 1;\nVALUES (N'1_First', N'10.0.10');\n", TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<MigrationScriptException>(
            () => ScriptFile.VerifyAsync(path, "SELECT 1;\nVALUES (N'1_First', N'10.0.14');\n", accept: false));

        Assert.Contains("is out of date with its migrations", error.Message);
        Assert.Contains("EF Core changed from 10.0.10 to 10.0.14", error.Message);
        Assert.Contains("@@ line 2 @@\n- VALUES (N'1_First', N'10.0.10');\n+ VALUES (N'1_First', N'10.0.14');\n", error.Message);
        Assert.Contains("SHIPSHAPE_ACCEPT=1", error.Message);
    }

    [Fact]
    public async Task VerifyAsync_ShouldThrow_WhenFileIsMissingAndAcceptIsOff()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");

        var error = await Assert.ThrowsAsync<MigrationScriptException>(() => ScriptFile.VerifyAsync(path, Generated, accept: false));

        Assert.Contains("doesn't exist yet", error.Message);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task VerifyAsync_ShouldCreateLfFileWithoutBom_WhenFileIsMissingAndAcceptIsOn()
    {
        using var folder = new TempFolder();
        var path = folder.File(Path.Combine("sql", "A.sql"));

        await ScriptFile.VerifyAsync(path, Generated, accept: true);

        Assert.Equal(Encoding.UTF8.GetBytes(Generated), await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task VerifyAsync_ShouldKeepCrlfAndBom_WhenAcceptingOverExistingFile()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");
        await File.WriteAllTextAsync(path, "OLD\r\n", new UTF8Encoding(true), TestContext.Current.CancellationToken);

        await ScriptFile.VerifyAsync(path, Generated, accept: true);

        var expected = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(Generated.ReplaceLineEndings("\r\n")));
        Assert.Equal(expected, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadBodyAsync_ShouldReturnTextAfterHeader_WhenFileStartsWithHeader()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");
        await File.WriteAllTextAsync(path, "USE [A];\r\nGO\r\n\r\n" + Generated, TestContext.Current.CancellationToken);

        var body = await ScriptFile.ReadBodyAsync(path, "USE [A];\nGO\n\n");

        Assert.Equal(Generated, body);
    }

    [Fact]
    public async Task ReadBodyAsync_ShouldThrow_WhenHeaderDiffers()
    {
        using var folder = new TempFolder();
        var path = folder.File("A.sql");
        await File.WriteAllTextAsync(path, "USE [B];\nGO\n\n" + Generated, TestContext.Current.CancellationToken);

        var error = await Assert.ThrowsAsync<MigrationScriptException>(() => ScriptFile.ReadBodyAsync(path, "USE [A];\nGO\n\n"));

        Assert.Contains("doesn't start with the header its options describe", error.Message);
    }
}
