using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Shipshape;

/// <summary>Explains a failed batch when the cause is custom SQL that an idempotent script can't run without EXEC.</summary>
internal static class SqlErrorExplainer
{
    // 207 and 208: an invalid column or object name, compiled before an earlier statement in the same batch created it.
    private static readonly int[] UnknownNameErrors = [207, 208];

    // 156 and 111: CREATE VIEW, PROCEDURE, FUNCTION or TRIGGER can't sit inside the script's IF ... BEGIN ... END.
    private static readonly int[] FirstInBatchErrors = [156, 111];

    public static string? Explain(DbContext context, ScriptBatchException failure)
    {
        var unknownName = UnknownNameErrors.Contains(failure.ErrorNumber);
        var notFirstInBatch = FirstInBatchErrors.Contains(failure.ErrorNumber) && NamesModule(failure.InnerException!.Message);
        if ((!unknownName && !notFirstInBatch) || failure.MigrationId is not { } id || !HasCustomSql(context, id))
        {
            return null;
        }

        var cause = unknownName
            ? "An idempotent script runs each migration as one batch, so SQL Server compiles its custom SQL before the "
              + "statements ahead of it have created the names it uses."
            : "An idempotent script puts every statement inside IF NOT EXISTS (...) BEGIN ... END, where CREATE VIEW, "
              + "PROCEDURE, FUNCTION and TRIGGER aren't allowed.";
        return $"Migration {id} fails in the idempotent script: {failure.InnerException!.Message} {cause} Add "
            + "WrapCustomSqlInExec() to the context's options, or change its migrationBuilder.Sql(...) to "
            + "migrationBuilder.SqlExec(...), then regenerate the script.";
    }

    // 156 is any syntax error near a keyword; only one near a module keyword comes from the IF ... BEGIN wrapper.
    private static bool NamesModule(string message) =>
        new[] { "VIEW", "PROC", "FUNCTION", "TRIGGER" }.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static bool HasCustomSql(DbContext context, string migrationId)
    {
        var assembly = context.GetService<IMigrationsAssembly>();
        if (!assembly.Migrations.TryGetValue(migrationId, out var type))
        {
            return false;
        }

        var migration = assembly.CreateMigration(type, context.Database.ProviderName!);
        return migration.UpOperations.OfType<SqlOperation>().Any();
    }
}
