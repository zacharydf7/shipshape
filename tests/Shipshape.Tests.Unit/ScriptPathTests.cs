using Shipshape.Tests.Toys;

namespace Shipshape.Tests.Unit;

public class ScriptPathTests
{
    [Fact]
    public void Resolve_ShouldFindFileInFolderAbove_WhenGivenRelativePath()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder.File(Path.Combine("deploy", "sql")));
        File.WriteAllText(folder.File(Path.Combine("deploy", "sql", "A.sql")), "");
        var start = Directory.CreateDirectory(folder.File(Path.Combine("tests", "bin", "Release"))).FullName;

        var path = ScriptPath.Resolve("deploy/sql/A.sql", start);

        Assert.Equal(folder.File(Path.Combine("deploy", "sql", "A.sql")), path);
    }

    [Fact]
    public void Resolve_ShouldUseExistingFolder_WhenFileDoesNotExistYet()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder.File(Path.Combine("deploy", "sql")));
        var start = Directory.CreateDirectory(folder.File("bin")).FullName;

        var path = ScriptPath.Resolve("deploy/sql/New.sql", start);

        Assert.Equal(folder.File(Path.Combine("deploy", "sql", "New.sql")), path);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenNeitherFileNorFolderExists()
    {
        using var folder = new TempFolder();

        var error = Assert.Throws<MigrationScriptException>(() => ScriptPath.Resolve("New.sql", folder.Path));

        Assert.Contains("Can't find 'New.sql'", error.Message);
    }
}
