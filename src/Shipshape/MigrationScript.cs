using Microsoft.EntityFrameworkCore;

namespace Shipshape;

/// <summary>
/// A checked-in deployment script and the context whose migrations it must match. Write two tests per script: one calls
/// <see cref="VerifyFileAsync"/>, the other <see cref="VerifyAgainstServerAsync"/>.
/// </summary>
public sealed class MigrationScript
{
    /// <summary>Set SHIPSHAPE_ACCEPT=1 to rewrite stale script files instead of failing. Ignored on CI.</summary>
    public const string AcceptVariable = "SHIPSHAPE_ACCEPT";

    private readonly DbContext _context;
    private readonly MigrationScriptOptions _options;

    private MigrationScript(DbContext context, string path, MigrationScriptOptions options)
    {
        _context = context;
        _options = options;
        FullPath = path;
    }

    /// <summary>The script file, after a relative path was found by searching upward from the test's output folder.</summary>
    public string FullPath { get; }

    /// <summary>
    /// Describes the script at <paramref name="path"/>. A relative path, such as <c>deploy/sql/Orders.sql</c>, is found by
    /// searching the test's output folder and the folders above it, so it can be written relative to the repository.
    /// </summary>
    public static MigrationScript For(DbContext context, string path, MigrationScriptOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new MigrationScriptOptions();
        if ((options.Header & (ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase)) != 0
            && string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            throw new ArgumentException(
                $"Set {nameof(MigrationScriptOptions.DatabaseName)}: the header creates or uses a database.", nameof(options));
        }

        return new MigrationScript(context, ScriptPath.Resolve(path), options);
    }

    /// <summary>
    /// Regenerates the script from the context's migrations, header included, and fails with a diff when the file differs.
    /// Needs no database. With SHIPSHAPE_ACCEPT=1 it rewrites the file instead, keeping its line endings and BOM.
    /// </summary>
    public Task VerifyFileAsync()
    {
        var script = _options.WrapCustomSqlInExec ? ScriptGenerator.GenerateWithExecWrapping(_context) : ScriptGenerator.Generate(_context);
        return ScriptFile.VerifyAsync(FullPath, Header(_options.DatabaseName) + script, AcceptMode.IsOn());
    }

    /// <summary>
    /// Applies the checked-in script and the context's migrations to two throwaway databases on the server, then runs every
    /// <see cref="ScriptCheck"/> not in <see cref="MigrationScriptOptions.Skip"/>. Both databases are dropped afterward.
    /// The context's connection string is changed while this runs and restored at the end.
    /// </summary>
    /// <param name="serverConnectionString">A connection string for the server; its database, if any, is not used.</param>
    public async Task VerifyAgainstServerAsync(string serverConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverConnectionString);
        var body = await ScriptFile.ReadBodyAsync(FullPath, Header(_options.DatabaseName));
        var problems = await ServerChecks.RunAsync(_context, body, Header, _options.Skip, serverConnectionString);
        if (problems.Count > 0)
        {
            throw new MigrationScriptException($"{FullPath} failed {problems.Count} check(s):\n\n" + string.Join("\n\n", problems));
        }
    }

    private string Header(string? databaseName) => ScriptHeaderWriter.Write(_options.Header, databaseName ?? "");
}
