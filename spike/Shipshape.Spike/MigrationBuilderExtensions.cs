using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Shipshape.Spike;

public static class MigrationBuilderExtensions
{
    /// <summary>
    /// Runs one batch of SQL inside <c>EXEC(N'...')</c>, so an idempotent script compiles it only when it runs, after the
    /// statements before it in the same migration. The SQL can't contain GO.
    /// </summary>
    public static OperationBuilder<SqlOperation> SqlExec(this MigrationBuilder migrationBuilder, string sql) =>
        migrationBuilder.Sql("EXEC(N'" + sql.Replace("'", "''") + "')");
}
