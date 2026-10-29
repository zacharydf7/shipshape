using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;

namespace Shipshape;

/// <summary>
/// Wraps each batch of custom SQL (<c>migrationBuilder.Sql</c>) in <c>EXEC(N'...')</c> when generating an idempotent
/// script, as EF already does for its own filtered indexes and computed columns. Migrate() and other scripts are unchanged.
/// Each EXEC is its own scope, so a #temp table one batch creates is gone in the next.
/// </summary>
internal sealed partial class ExecWrappingMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies, ICommandBatchPreparer commandBatchPreparer)
    : SqlServerMigrationsSqlGenerator(dependencies, commandBatchPreparer)
{
    protected override void Generate(SqlOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (!Options.HasFlag(MigrationsSqlGenerationOptions.Idempotent))
        {
            base.Generate(operation, model, builder);
            return;
        }

        // EF splits the SQL on GO first, so each batch is wrapped on its own.
        var batches = new MigrationCommandListBuilder(Dependencies);
        base.Generate(operation, model, batches);
        foreach (var batch in batches.GetCommandList())
        {
            var sql = batch.CommandText.TrimEnd();
            if (AlreadyExec().IsMatch(sql))
            {
                builder.Append(batch.CommandText);
            }
            else
            {
                builder.Append("EXEC(N'" + sql.Replace("'", "''") + "')").AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            }

            builder.EndCommand(batch.TransactionSuppressed);
        }
    }

    // SQL someone already wrapped by hand stays as it is, so turning this on doesn't rewrite those scripts.
    [GeneratedRegex(@"^\s*EXEC(UTE)?\s*\(\s*N?'(?:[^']|'')*'\s*\)\s*;?$", RegexOptions.IgnoreCase)]
    private static partial Regex AlreadyExec();
}
