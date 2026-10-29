using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shipshape.Tests.Toys;

// The toy migrations are hand-written in the shape EF generates, without model snapshots except Drift's.
public sealed class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options);

// Catalog and Billing share the Store database; Billing keeps its own history table.
public sealed class CatalogContext(DbContextOptions<CatalogContext> options) : DbContext(options);

public sealed class BillingContext(DbContextOptions<BillingContext> options) : DbContext(options);

// Planner's custom SQL is written the naive way, without EXEC, so it needs WrapCustomSqlInExec.
public sealed class PlannerContext(DbContextOptions<PlannerContext> options) : DbContext(options);

// The same problem as Planner, fixed with the SqlExec helper.
public sealed class ChecklistContext(DbContextOptions<ChecklistContext> options) : DbContext(options);

// A model that gained Note.Pinned after its last migration, so it has pending model changes.
public sealed class DriftContext(DbContextOptions<DriftContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

public sealed class Note
{
    public int Id { get; set; }

    public string Text { get; set; } = "";

    public bool Pinned { get; set; }
}

public static class ToyContexts
{
    // Nothing here should connect: generating a script needs no database.
    public const string Unreachable = "Server=unreachable;Database=Toy;Connect Timeout=1";

    /// <summary>Planner always wraps its custom SQL in EXEC; <paramref name="wrapCustomSql"/> turns it on for the others.</summary>
    public static DbContext Create(string name, IInterceptor? interceptor = null, bool wrapCustomSql = false) => name switch
    {
        "Library" => new LibraryContext(Options<LibraryContext>(interceptor, wrapCustomSql)),
        "Catalog" => new CatalogContext(Options<CatalogContext>(interceptor, wrapCustomSql)),
        "Billing" => new BillingContext(Options<BillingContext>(interceptor, wrapCustomSql, "__BillingHistory", "billing")),
        "Planner" => new PlannerContext(Options<PlannerContext>(interceptor, wrapCustomSql: true)),
        "Checklist" => new ChecklistContext(Options<ChecklistContext>(interceptor, wrapCustomSql)),
        "Drift" => new DriftContext(Options<DriftContext>(interceptor, wrapCustomSql)),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Not a toy context."),
    };

    /// <summary>Planner without <c>WrapCustomSqlInExec</c>, whose idempotent script fails.</summary>
    public static PlannerContext UnwrappedPlanner() => new(Options<PlannerContext>(null, wrapCustomSql: false));

    /// <summary>Library with a seeder, which adds a row when Migrate() runs and never when the script does.</summary>
    public static LibraryContext LibraryWithSeeding()
    {
        const string insert = "INSERT INTO [Genres] ([Id], [Name]) VALUES (9, N'Seeded')";
        return new(new DbContextOptionsBuilder<LibraryContext>()
            .UseSqlServer(Unreachable)
            .UseSeeding((context, _) => context.Database.ExecuteSqlRaw(insert))
            .UseAsyncSeeding((context, _, cancellationToken) => context.Database.ExecuteSqlRawAsync(insert, cancellationToken))
            .Options);
    }

    /// <summary>Drift set up the way skipping its pending model changes requires, so Migrate() still runs.</summary>
    public static DriftContext DriftIgnoringPendingModelChanges() => new(new DbContextOptionsBuilder<DriftContext>()
        .UseSqlServer(Unreachable)
        .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
        .Options);

    /// <summary>The options each toy's checked-in script under tests/Scripts was generated with.</summary>
    public static MigrationScriptOptions ScriptOptions(string name) => name switch
    {
        "Catalog" or "Billing" => new() { DatabaseName = "Store", Header = ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase },
        _ => new() { DatabaseName = name, Header = ScriptHeader.QuotedIdentifierOn | ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase },
    };

    public static string ScriptPath(string name) => $"Scripts/{name}.sql";

    private static DbContextOptions<T> Options<T>(IInterceptor? interceptor, bool wrapCustomSql, string? historyTable = null,
        string? historySchema = null)
        where T : DbContext
    {
        var builder = new DbContextOptionsBuilder<T>()
            .UseSqlServer(Unreachable, sql =>
            {
                if (historyTable is not null)
                {
                    sql.MigrationsHistoryTable(historyTable, historySchema);
                }
            });
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return wrapCustomSql ? builder.WrapCustomSqlInExec().Options : builder.Options;
    }
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
