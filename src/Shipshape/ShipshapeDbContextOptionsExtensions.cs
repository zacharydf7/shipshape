using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shipshape;

public static class ShipshapeDbContextOptionsExtensions
{
    /// <summary>
    /// Wraps each batch of custom SQL (<c>migrationBuilder.Sql</c>) in <c>EXEC(N'...')</c> whenever EF generates an
    /// idempotent script, so SQL that uses a column added earlier in the same migration, or creates a view or procedure,
    /// runs there too. Migrate(), bundles and other scripts are unchanged. Add it where the context is configured, so
    /// <c>dotnet ef migrations script</c> and Shipshape produce the same script.
    /// </summary>
    /// <remarks>Each EXEC is its own scope: a #temp table one batch creates is gone in the next.</remarks>
    public static DbContextOptionsBuilder WrapCustomSqlInExec(this DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, ExecWrappingMigrationsSqlGenerator>();

    /// <inheritdoc cref="WrapCustomSqlInExec(DbContextOptionsBuilder)"/>
    public static DbContextOptionsBuilder<TContext> WrapCustomSqlInExec<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext =>
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, ExecWrappingMigrationsSqlGenerator>();
}
