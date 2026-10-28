using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape.Spike;

/// <summary>Generates a context's idempotent script in-process, through the API `dotnet ef migrations script` calls.</summary>
public static class ScriptGenerator
{
    // EF ends lines with Environment.NewLine, so scripts are normalized to LF to compare the same on every system.
    public static string Generate(DbContext context, string? fromMigration = null, string? toMigration = null) =>
        context.GetService<IMigrator>()
            .GenerateScript(fromMigration, toMigration, MigrationsSqlGenerationOptions.Idempotent)
            .ReplaceLineEndings("\n");

    /// <summary>
    /// Builds a second instance of the context with every option it already has, plus the generator that wraps custom
    /// SQL in EXEC. Needs a constructor that takes the context's options.
    /// </summary>
    public static DbContext WithExecWrapping(DbContext context)
    {
        var options = new DbContextOptionsBuilder((DbContextOptions)context.GetService<IDbContextOptions>())
            .ReplaceService<IMigrationsSqlGenerator, ExecWrappingMigrationsSqlGenerator>()
            .Options;

        return (DbContext)Activator.CreateInstance(context.GetType(), options)!;
    }
}
