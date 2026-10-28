using Microsoft.EntityFrameworkCore;

namespace Shipshape.Spike;

public enum ScriptCheck
{
    AppliesToEmptyDatabase,
    MatchesMigratedSchema,
    RecordsEveryMigration,
    ChangesNothingOnRerun,
}

public sealed record ParityProblem(ScriptCheck Check, string Message);

/// <summary>
/// Applies a script and the context's migrations to two throwaway databases, then reports every way the script falls
/// short: it fails, builds a different database, leaves migrations unrecorded, or changes something when run again.
/// The script must be EF's output without a header: a USE would run it outside the throwaway database.
/// </summary>
public static class ParityCheck
{
    public static async Task<IReadOnlyList<ParityProblem>> RunAsync(DbContext context, string script, string serverConnectionString)
    {
        var problems = new List<ParityProblem>();
        await using var fromScript = await ThrowawayDatabase.CreateAsync(serverConnectionString);
        await using var fromMigrations = await ThrowawayDatabase.CreateAsync(serverConnectionString);

        // Migrating first means a migration that fails everywhere throws here, so the explainer below only sees
        // failures that happen in the idempotent script alone.
        context.Database.SetConnectionString(fromMigrations.ConnectionString);
        await context.Database.MigrateAsync();

        try
        {
            await ScriptRunner.RunAsync(fromScript.ConnectionString, script);
        }
        catch (ScriptBatchException e)
        {
            problems.Add(new(ScriptCheck.AppliesToEmptyDatabase, SqlErrorExplainer.Explain(context, e) ?? e.Message));
            return problems;
        }

        var migrated = await SchemaSnapshot.TakeAsync(fromMigrations.ConnectionString);
        var scripted = await SchemaSnapshot.TakeAsync(fromScript.ConnectionString);
        foreach (var difference in SchemaSnapshot.Compare(migrated, scripted))
        {
            problems.Add(new(ScriptCheck.MatchesMigratedSchema, difference));
        }

        context.Database.SetConnectionString(fromScript.ConnectionString);
        foreach (var pending in await context.Database.GetPendingMigrationsAsync())
        {
            problems.Add(new(ScriptCheck.RecordsEveryMigration, $"{pending} is not recorded in the history table"));
        }

        var beforeRerun = await SchemaSnapshot.TakeAsync(fromScript.ConnectionString, includeChecksums: true);
        try
        {
            await ScriptRunner.RunAsync(fromScript.ConnectionString, script);
        }
        catch (ScriptBatchException e)
        {
            problems.Add(new(ScriptCheck.ChangesNothingOnRerun, "The second run failed. " + e.Message));
            return problems;
        }

        var afterRerun = await SchemaSnapshot.TakeAsync(fromScript.ConnectionString, includeChecksums: true);
        foreach (var difference in SchemaSnapshot.Compare(beforeRerun, afterRerun))
        {
            problems.Add(new(ScriptCheck.ChangesNothingOnRerun, difference));
        }

        return problems;
    }
}
