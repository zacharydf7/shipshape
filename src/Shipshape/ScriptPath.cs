namespace Shipshape;

internal static class ScriptPath
{
    /// <summary>
    /// Finds a relative path in the test's output folder or the nearest folder above it that holds the file, or else its
    /// folder, so a test can name a script relative to the repository.
    /// </summary>
    public static string Resolve(string path, string? startFolder = null)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var folders = Ancestors(startFolder ?? AppContext.BaseDirectory).ToList();
        var existing = folders.Select(folder => Path.Combine(folder, path)).FirstOrDefault(File.Exists);
        if (existing is not null)
        {
            return Path.GetFullPath(existing);
        }

        var scriptFolder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(scriptFolder))
        {
            var parent = folders.FirstOrDefault(folder => Directory.Exists(Path.Combine(folder, scriptFolder)));
            if (parent is not null)
            {
                return Path.GetFullPath(Path.Combine(parent, path));
            }
        }

        throw new MigrationScriptException($"Can't find '{path}' in {folders[0]} or any folder above it. Pass a path whose "
            + "folder exists, such as deploy/sql/Orders.sql, or an absolute path.");
    }

    private static IEnumerable<string> Ancestors(string folder)
    {
        for (var current = new DirectoryInfo(folder); current is not null; current = current.Parent)
        {
            yield return current.FullName;
        }
    }
}
