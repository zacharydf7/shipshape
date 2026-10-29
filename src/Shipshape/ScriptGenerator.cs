using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape;

/// <summary>Generates a context's idempotent script in-process, through the API `dotnet ef migrations script` calls.</summary>
internal static class ScriptGenerator
{
    // EF ends lines with Environment.NewLine, so scripts are normalized to LF to compare the same on every system.
    public static string Generate(DbContext context, string? fromMigration = null, string? toMigration = null) =>
        context.GetService<IMigrator>()
            .GenerateScript(fromMigration, toMigration, MigrationsSqlGenerationOptions.Idempotent)
            .ReplaceLineEndings("\n");

    /// <summary>
    /// Generates the script from a copy of the context that wraps custom SQL in EXEC, keeping every other option, for a
    /// context whose options can't take <see cref="ShipshapeDbContextOptionsExtensions.WrapCustomSqlInExec(DbContextOptionsBuilder)"/>.
    /// </summary>
    public static string GenerateWithExecWrapping(DbContext context)
    {
        var options = new DbContextOptionsBuilder((DbContextOptions)context.GetService<IDbContextOptions>())
            .WrapCustomSqlInExec()
            .Options;
        if (context.GetType().GetConstructor([options.GetType()]) is not { } constructor)
        {
            throw new MigrationScriptException($"{nameof(MigrationScriptOptions.WrapCustomSqlInExec)} needs {context.GetType().Name} to have "
                + "a constructor that takes only its options. Add WrapCustomSqlInExec() where the context is configured, or "
                + "use migrationBuilder.SqlExec(...) in its migrations.");
        }

        using var wrapping = (DbContext)constructor.Invoke([options]);
        return Generate(wrapping);
    }
}
