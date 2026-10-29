# Shipshape

Keeps EF Core's idempotent SQL Server deployment scripts generated, current and proven.

EF Core's docs recommend SQL scripts when a DBA or an installer applies your migrations, but nothing keeps those scripts
correct. Shipshape is a test library for that case. You write two tests per checked-in `.sql` file:

- **The script is current.** One test regenerates the script from your `DbContext`, header included, with no database.
  A stale script fails with a diff. Set `SHIPSHAPE_ACCEPT=1` and run the test again to rewrite the file.
- **The script builds the right database.** Another test applies the script and your migrations to two throwaway SQL
  Server databases. It checks that they match, that every migration is recorded, and that a second run changes nothing.

Requires EF Core 10 with SQL Server. Shipshape's major version follows EF Core's.

## The two tests

```csharp
public class OrdersScriptTests
{
    private static readonly MigrationScriptOptions Options = new()
    {
        DatabaseName = "ShopOrders",
        Header = ScriptHeader.QuotedIdentifierOn | ScriptHeader.CreateDatabaseIfMissing | ScriptHeader.UseDatabase,
    };

    [Fact]
    public async Task OrdersScript_ShouldMatchTheMigrations_WhenRegenerated()
    {
        await using OrdersDbContext context = TestContexts.Orders(); // any SQL Server context; nothing is opened

        await MigrationScript.For(context, "deploy/sql/Orders.sql", Options).VerifyFileAsync();
    }

    [Fact]
    public async Task OrdersScript_ShouldBuildTheMigratedSchema_WhenAppliedTwice()
    {
        await using OrdersDbContext context = TestContexts.Orders();

        await MigrationScript.For(context, "deploy/sql/Orders.sql", Options).VerifyAgainstServerAsync(SqlServerConnectionString);
    }
}
```

A relative path is found by searching the test's output folder and every folder above it, so you can write it relative
to the repository. Failures throw `MigrationScriptException`, so this works the same under xUnit, NUnit and MSTest.

## Keeping the file current

`VerifyFileAsync` generates the script in-process with the API `dotnet ef migrations script --idempotent` uses, then
compares it with the file, ignoring line endings and a BOM. When they differ, the test fails like this:

```
deploy/sql/Orders.sql is out of date with its migrations.

@@ line 412 @@
+ IF NOT EXISTS (
+     SELECT * FROM [__EFMigrationsHistory]
+     WHERE [MigrationId] = N'20261101090000_AddOrderNotes'
...
To update the file, set SHIPSHAPE_ACCEPT=1 and run the test again.
```

When the script's history inserts name another EF Core version, the message starts with "EF Core changed from X to Y",
because an EF upgrade changes generated SQL in many places at once.

With `SHIPSHAPE_ACCEPT=1` (or `true`) the test rewrites the file instead, keeping its line endings and BOM so only the
changed lines show in the diff. A new file is written with LF line endings and no BOM. Accepting never happens on a build
server: when `CI` or `TF_BUILD` is set, a stale script always fails.

### The header

The header is generated too, so you never paste it back by hand. Its statements come from `ScriptHeader` flags and are
always written in this order:

| Flag | Writes |
| --- | --- |
| `QuotedIdentifierOn` | `SET QUOTED_IDENTIFIER ON;`, which sqlcmd turns off and filtered indexes need |
| `CreateDatabaseIfMissing` | `IF DB_ID(N'ShopOrders') IS NULL CREATE DATABASE [ShopOrders];` |
| `UseDatabase` | `USE [ShopOrders];` |

The default, `ScriptHeader.None`, writes nothing and matches `dotnet ef`'s own output.

## Proving the script

`VerifyAgainstServerAsync` takes a connection string for a SQL Server (LocalDB, a CI service container, Testcontainers;
its database part is ignored). It creates two databases named `shipshape_<guid>` and drops them afterward. It runs
`Migrate()` on one, applies the checked-in script to the other with its header renamed to that database, and runs every
check:

| `ScriptCheck` | Fails when |
| --- | --- |
| `AppliesToEmptyDatabase` | a batch errors on the empty database |
| `MatchesMigratedSchema` | the scripted database differs from the migrated one |
| `RecordsEveryMigration` | `GetPendingMigrations()` isn't empty after the script ran |
| `NoPendingModelChanges` | `HasPendingModelChanges()` is true: a model edit with no migration |
| `ChangesNothingOnRerun` | a second run fails, or changes the schema or any row |

All failures are collected into one message, and each says what to do about it. The context's connection string is
restored when the test finishes.

Skip a check with `Skip = { ScriptCheck.NoPendingModelChanges }`, for example for a context owned by another package.
Since EF Core 9, `Migrate()` itself refuses to run while the model has pending changes, so that skip also needs
`ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))` on the context the test uses.
`AppliesToEmptyDatabase` can't really be skipped: when the script fails, nothing else can be checked, so the failure is
always reported.

### What the comparison covers

Both databases are described from SQL Server's catalog views as sorted lines of text, and the lines are compared:

- schemas, tables (with their temporal history table) and column order;
- columns: type, length, precision, nullability, collation, identity, computed definition, default definition, and
  sparse, hidden or rowguidcol flags;
- indexes: kind, uniqueness, key columns and their direction, included columns, filter;
- primary keys, unique constraints, foreign keys with their delete and update actions, check constraints;
- sequences, and the definitions of views, procedures, functions and triggers;
- the migration IDs in every history table, and each table's row count. Row counts are left out when the context
  configures `UseSeeding` or `UseAsyncSeeding`, because `Migrate()` runs those and the script doesn't.

Names SQL Server makes up, such as `DF__Orders__Status__3B75D760`, are left out, so two builds of the same schema compare
equal. Not compared: synonyms, full-text indexes, partitioning, permissions and other server objects.

## Custom SQL that needs EXEC

An idempotent script runs each migration as one batch, inside `IF NOT EXISTS (...) BEGIN ... END`. Two kinds of
`migrationBuilder.Sql` therefore fail there, although `Migrate()` runs them fine:

- SQL that uses a column added earlier in the same migration: SQL Server compiles the whole batch first, and reports
  "Invalid column name".
- `CREATE VIEW`, `PROCEDURE`, `FUNCTION` or `TRIGGER`, which can't sit inside `BEGIN ... END`.

`AppliesToEmptyDatabase` names the migration and the fix. EF wraps its own filtered indexes, computed columns and data
changes in `EXEC(N'...')` for exactly this reason. To have it do the same for your custom SQL, add one line where the
context is configured:

```csharp
options.UseSqlServer(connectionString).WrapCustomSqlInExec();
```

It changes only idempotent scripts, so `Migrate()` and bundles behave as before, and `dotnet ef migrations script` then
produces the same script Shipshape does. SQL you already wrapped in `EXEC(N'...')` is left alone. Each wrapped batch is
its own scope, so a `#temp` table created in one batch is gone in the next.

To fix a single migration instead, call `migrationBuilder.SqlExec("...")` in place of `migrationBuilder.Sql("...")`.

## Known EF behavior worth knowing

In an idempotent script EF indents every line of custom SQL, including lines inside an `EXEC(N'...')` string. A view or
procedure created that way is stored with different leading whitespace than `Migrate()` gives it, so the comparison
ignores leading and trailing whitespace on each line of a definition. A multi-line string literal in a backfill gets the
same indentation in its data, and no check can see that.

## Prior art

- [EF Core migration bundles](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) apply
  migrations without scripts. Use them when nobody needs to review SQL.
- [EfCore.SchemaCompare](https://github.com/JonPSmith/EfCore.SchemaCompare) compares a model with a live database.
  Shipshape compares a script file with the migrations; the two complement each other.
- [DbUp](https://dbup.readthedocs.io/) and [grate](https://github.com/erikbra/grate) apply scripts. Shipshape keeps
  the scripts they apply correct.

## License

MIT
