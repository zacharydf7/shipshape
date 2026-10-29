namespace Shipshape;

/// <summary>The checks <see cref="MigrationScript.VerifyAgainstServerAsync"/> runs. Every one runs unless skipped.</summary>
public enum ScriptCheck
{
    /// <summary>
    /// The script runs on an empty database without an error. A failure is reported even when this is skipped, because no
    /// other check can run on a script that fails.
    /// </summary>
    AppliesToEmptyDatabase,

    /// <summary>The script builds the same tables, columns, indexes, keys, constraints, sequences and views as Migrate().</summary>
    MatchesMigratedSchema,

    /// <summary>After the script, the history table records every migration, so none is pending.</summary>
    RecordsEveryMigration,

    /// <summary>
    /// The model has no changes that lack a migration. Since EF Core 9, Migrate() itself refuses to run while there are
    /// such changes, so skipping this also needs <c>RelationalEventId.PendingModelChangesWarning</c> ignored in the
    /// context's <c>ConfigureWarnings</c>.
    /// </summary>
    NoPendingModelChanges,

    /// <summary>Running the script a second time changes neither the schema nor any rows.</summary>
    ChangesNothingOnRerun,
}
