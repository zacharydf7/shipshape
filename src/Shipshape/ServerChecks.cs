using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Shipshape;

/// <summary>
/// Applies a script and the context's migrations to two throwaway databases and reports every check that fails, each as
/// a paragraph that names the fix.
/// </summary>
internal static class ServerChecks
{
    private const string Regenerate = $"Regenerate the script: set {MigrationScript.AcceptVariable}=1 and run its file test.";

    private const string IgnoreWarning = "ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))";

    public static async Task<IReadOnlyList<string>> RunAsync(DbContext context, string body, Func<string, string> header,
        ISet<ScriptCheck> skip, string serverConnectionString)
    {
        var problems = new List<string>();
        void Report(ScriptCheck check, string message)
        {
            if (!skip.Contains(check))
            {
                problems.Add($"{check}: {message}");
            }
        }

        var pendingModelChanges = context.Database.HasPendingModelChanges();
        if (pendingModelChanges)
        {
            Report(ScriptCheck.NoPendingModelChanges, "The model has changes that no migration covers. Add a migration "
                + "(dotnet ef migrations add), then regenerate the script.");
        }

        var originalConnectionString = context.Database.GetConnectionString();
        try
        {
            await using var fromScript = await ThrowawayDatabase.CreateAsync(serverConnectionString);
            await using var fromMigrations = await ThrowawayDatabase.CreateAsync(serverConnectionString);

            // Migrating first means a migration that fails everywhere is reported as such, not blamed on the script.
            context.Database.SetConnectionString(fromMigrations.ConnectionString);
            try
            {
                await context.Database.MigrateAsync();
            }
            catch (InvalidOperationException e) when (pendingModelChanges
                && e.Message.Contains(nameof(RelationalEventId.PendingModelChangesWarning), StringComparison.Ordinal))
            {
                // Since EF Core 9, Migrate() refuses to run while the model has pending changes, unless told otherwise.
                problems.Add(skip.Contains(ScriptCheck.NoPendingModelChanges)
                    ? $"{ScriptCheck.NoPendingModelChanges} is skipped, but Migrate() still refuses to run while the model has "
                      + $"pending changes. Add {IgnoreWarning} to the options of the context this test uses."
                    : "The other checks need Migrate(), which refuses to run while the model has pending changes.");
                return problems;
            }

            // The header is renamed to the throwaway database, so its USE can't reach the real one.
            var script = header(fromScript.Name) + body;
            try
            {
                await ScriptRunner.RunAsync(fromScript.ConnectionString, script);
            }
            catch (ScriptBatchException e)
            {
                // Reported even when skipped: no other check can run on a script that fails.
                problems.Add($"{ScriptCheck.AppliesToEmptyDatabase}: " + (SqlErrorExplainer.Explain(context, e)
                    ?? $"{e.Message} Fix the migration or the hand edit that causes it, then regenerate the script."));
                return problems;
            }

            var migrated = await SchemaSnapshot.TakeAsync(fromMigrations.ConnectionString);
            var scripted = await SchemaSnapshot.TakeAsync(fromScript.ConnectionString);
            if (HasSeeder(context))
            {
                // UseSeeding runs inside Migrate() only, so the migrated database has rows the script never adds.
                migrated = WithoutRowCounts(migrated);
                scripted = WithoutRowCounts(scripted);
            }

            if (SchemaSnapshot.Compare(migrated, scripted) is { Count: > 0 } differences)
            {
                Report(ScriptCheck.MatchesMigratedSchema, "The script builds a different database than Migrate(); - is only "
                    + "in the migrated one, + only in the scripted one.\n" + string.Join('\n', differences) + "\n" + Regenerate);
            }

            context.Database.SetConnectionString(fromScript.ConnectionString);
            if ((await context.Database.GetPendingMigrationsAsync()).ToList() is { Count: > 0 } pending)
            {
                Report(ScriptCheck.RecordsEveryMigration, $"After the script ran, {string.Join(", ", pending)} still "
                    + "count as pending: the script doesn't record them in the history table. " + Regenerate);
            }

            if (!skip.Contains(ScriptCheck.ChangesNothingOnRerun))
            {
                await CheckRerunAsync(fromScript.ConnectionString, script, problems);
            }

            return problems;
        }
        finally
        {
            context.Database.SetConnectionString(originalConnectionString);
        }
    }

    private static async Task CheckRerunAsync(string connectionString, string script, List<string> problems)
    {
        var beforeRerun = await SchemaSnapshot.TakeAsync(connectionString, includeChecksums: true);
        try
        {
            await ScriptRunner.RunAsync(connectionString, script);
        }
        catch (ScriptBatchException e)
        {
            problems.Add($"{ScriptCheck.ChangesNothingOnRerun}: The second run failed. {e.Message} Every statement must be "
                + "skipped on a second run; check custom SQL for statements without a guard.");
            return;
        }

        var afterRerun = await SchemaSnapshot.TakeAsync(connectionString, includeChecksums: true);
        if (SchemaSnapshot.Compare(beforeRerun, afterRerun) is { Count: > 0 } changes)
        {
            problems.Add($"{ScriptCheck.ChangesNothingOnRerun}: The second run changed the database; - is before it, + after.\n"
                + string.Join('\n', changes) + "\nCheck custom SQL for inserts or updates without a guard.");
        }
    }

    private static bool HasSeeder(DbContext context) =>
        context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>() is { } core
        && (core.Seeder is not null || core.AsyncSeeder is not null);

    private static IReadOnlyList<string> WithoutRowCounts(IReadOnlyList<string> facts) =>
        facts.Where(fact => !fact.StartsWith("rows ", StringComparison.Ordinal)).ToList();
}
