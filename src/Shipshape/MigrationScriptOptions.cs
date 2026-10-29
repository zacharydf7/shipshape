namespace Shipshape;

public sealed class MigrationScriptOptions
{
    /// <summary>The database the header creates and uses. Needed when <see cref="Header"/> creates or uses one.</summary>
    public string? DatabaseName { get; init; }

    /// <summary>The statements written before EF's script. None by default, which matches <c>dotnet ef</c>'s own output.</summary>
    public ScriptHeader Header { get; init; }

    /// <summary>
    /// Checks <see cref="MigrationScript.VerifyAgainstServerAsync"/> leaves out, for example
    /// <see cref="ScriptCheck.NoPendingModelChanges"/> for a context another package owns. Each check's own remarks say
    /// what else skipping it needs.
    /// </summary>
    public ISet<ScriptCheck> Skip { get; } = new HashSet<ScriptCheck>();
}
