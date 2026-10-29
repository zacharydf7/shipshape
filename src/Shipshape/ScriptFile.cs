using System.Text;

namespace Shipshape;

/// <summary>Reads, compares and rewrites a checked-in script, ignoring the line endings and BOM it was saved with.</summary>
internal static class ScriptFile
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    public static async Task VerifyAsync(string path, string generated, bool accept)
    {
        var existing = File.Exists(path) ? await ReadAsync(path) : null;
        if (existing?.Text == generated)
        {
            return;
        }

        if (accept)
        {
            // Keep the file's own style, so accepting changes only the lines that changed.
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var text = existing?.Crlf == true ? generated.ReplaceLineEndings("\r\n") : generated;
            await File.WriteAllTextAsync(path, text, new UTF8Encoding(existing?.HasBom ?? false));
            return;
        }

        throw new MigrationScriptException(existing is null
            ? $"{path} doesn't exist yet. To create it, set {MigrationScript.AcceptVariable}=1 and run the test again."
            : StaleMessage(path, existing.Text, generated));
    }

    /// <summary>The script after its header, which must be the header the options describe.</summary>
    public static async Task<string> ReadBodyAsync(string path, string header)
    {
        if (!File.Exists(path))
        {
            throw new MigrationScriptException(
                $"{path} doesn't exist yet. Run its file test with {MigrationScript.AcceptVariable}=1 to create it.");
        }

        var text = (await ReadAsync(path)).Text;
        if (!text.StartsWith(header, StringComparison.Ordinal))
        {
            throw new MigrationScriptException($"{path} doesn't start with the header its options describe. Run its file "
                + "test first, with the same options, to bring the file up to date.");
        }

        return text[header.Length..];
    }

    private static string StaleMessage(string path, string file, string generated)
    {
        var message = new StringBuilder($"{path} is out of date with its migrations.\n");
        if (ScriptDiff.EfVersionChange(file, generated) is { } versionChange)
        {
            message.Append(versionChange).Append('\n');
        }

        message.Append('\n').Append(ScriptDiff.Describe(file, generated)).Append('\n');
        message.Append($"To update the file, set {MigrationScript.AcceptVariable}=1 and run the test again.");
        return message.ToString();
    }

    private static async Task<Contents> ReadAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        var hasBom = bytes.AsSpan().StartsWith(Bom);
        var text = Encoding.UTF8.GetString(hasBom ? bytes[Bom.Length..] : bytes);
        return new Contents(text.ReplaceLineEndings("\n"), hasBom, text.Contains("\r\n"));
    }

    private sealed record Contents(string Text, bool HasBom, bool Crlf);
}
