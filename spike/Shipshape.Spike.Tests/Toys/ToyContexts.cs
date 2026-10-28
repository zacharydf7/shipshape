using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shipshape.Spike.Tests.Toys;

// The toy migrations are hand-written in the shape EF generates, without model snapshots.
public sealed class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options);

// Catalog and Billing share the Store database; Billing keeps its own history table.
public sealed class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options);

public sealed class BillingContext(DbContextOptions<BillingContext> options) : DbContext(options);

// Planner's custom SQL is written the naive way, without EXEC.
public sealed class PlannerContext(DbContextOptions<PlannerContext> options) : DbContext(options);

// The same problem as Planner, fixed with the SqlExec helper.
public sealed class ChecklistContext(DbContextOptions<ChecklistContext> options) : DbContext(options);

public static class ToyContexts
{
    // Nothing here should connect: generating a script needs no database.
    public const string Unreachable = "Server=unreachable;Database=Toy;Connect Timeout=1";

    public static readonly string[] Names = ["Library", "Catalog", "Billing", "Planner"];

    public static DbContext Create(string name, params IInterceptor[] interceptors) => name switch
    {
        "Library" => new LibraryContext(Options<LibraryContext>(interceptors)),
        "Catalog" => new CatalogContext(Options<CatalogContext>(interceptors)),
        "Billing" => new BillingContext(Options<BillingContext>(interceptors, "__BillingHistory", "billing")),
        "Planner" => new PlannerContext(Options<PlannerContext>(interceptors)),
        "Checklist" => new ChecklistContext(Options<ChecklistContext>(interceptors)),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Not a toy context."),
    };

    private static DbContextOptions<T> Options<T>(IInterceptor[] interceptors, string? historyTable = null, string? historySchema = null)
        where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseSqlServer(Unreachable, sql =>
            {
                if (historyTable is not null)
                {
                    sql.MigrationsHistoryTable(historyTable, historySchema);
                }
            })
            .AddInterceptors(interceptors)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
}

public sealed class LibraryContextFactory : IDesignTimeDbContextFactory<LibraryContext>
{
    public LibraryContext CreateDbContext(string[] args) => (LibraryContext)ToyContexts.Create("Library");
}

public sealed class CatalogContextFactory : IDesignTimeDbContextFactory<CatalogContext>
{
    public CatalogContext CreateDbContext(string[] args) => (CatalogContext)ToyContexts.Create("Catalog");
}

public sealed class BillingContextFactory : IDesignTimeDbContextFactory<BillingContext>
{
    public BillingContext CreateDbContext(string[] args) => (BillingContext)ToyContexts.Create("Billing");
}

public sealed class PlannerContextFactory : IDesignTimeDbContextFactory<PlannerContext>
{
    public PlannerContext CreateDbContext(string[] args) => (PlannerContext)ToyContexts.Create("Planner");
}
