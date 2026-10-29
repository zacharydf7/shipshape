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
}
