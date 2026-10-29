using System.Text;
using System.Text.RegularExpressions;

namespace Shipshape;

/// <summary>Describes how a checked-in script differs from the regenerated one, for a failure message.</summary>
internal static partial class ScriptDiff
{
    private const int MaxLinesPerSide = 40;

    /// <summary>
    /// The changed region between the first and last differing line, marked - for the file and + for the regenerated
    /// script. A stale script usually differs in one place (new migrations at the end), which this shows exactly.
    /// </summary>
    public static string Describe(string file, string generated)
    {
        var oldLines = file.Split('\n');
        var newLines = generated.Split('\n');
        var start = 0;
        while (start < oldLines.Length && start < newLines.Length && oldLines[start] == newLines[start])
        {
            start++;
        }

        var end = 0;
        while (end < oldLines.Length - start && end < newLines.Length - start
            && oldLines[^(end + 1)] == newLines[^(end + 1)])
        {
            end++;
        }

        var text = new StringBuilder($"@@ line {start + 1} @@\n");
        AppendSide(text, "- ", oldLines[start..^end]);
        AppendSide(text, "+ ", newLines[start..^end]);
        return text.ToString();
    }

    /// <summary>"EF Core changed from X to Y" when the script's history inserts name another EF version, else null.</summary>
    public static string? EfVersionChange(string file, string generated) =>
        (LastEfVersion(file), LastEfVersion(generated)) is ({ } before, { } after) && before != after
            ? $"EF Core changed from {before} to {after}, and its generated SQL changes with it."
            : null;

    private static void AppendSide(StringBuilder text, string marker, string[] lines)
    {
        foreach (var line in lines.Take(MaxLinesPerSide))
        {
            text.Append(marker).Append(line).Append('\n');
        }

        if (lines.Length > MaxLinesPerSide)
        {
            text.Append(marker).Append($"... {lines.Length - MaxLinesPerSide} more lines\n");
        }
    }

    private static string? LastEfVersion(string script) =>
        HistoryInsert().Matches(script) is { Count: > 0 } matches ? matches[^1].Groups[1].Value : null;

    // The version EF writes into every history insert: VALUES (N'<migration>', N'<version>');
    [GeneratedRegex(@"VALUES \(N'[^']*', N'([^']+)'\);")]
    private static partial Regex HistoryInsert();
}
