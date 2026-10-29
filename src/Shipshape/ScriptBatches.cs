using System.Text;

namespace Shipshape;

/// <summary>Splits a script on its GO lines, ignoring GO inside strings, bracketed names and block comments.</summary>
internal static class ScriptBatches
{
    private enum State { Normal, InString, InBrackets, InDoubleQuotes, InBlockComment }

    public static IReadOnlyList<string> Split(string script)
    {
        var batches = new List<string>();
        var batch = new StringBuilder();
        var state = State.Normal;
        foreach (var line in script.ReplaceLineEndings("\n").Split('\n'))
        {
            if (state == State.Normal && line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                Add(batches, batch);
                continue;
            }

            state = Scan(line, state);
            batch.Append(line).Append('\n');
        }

        Add(batches, batch);
        return batches;
    }

    private static void Add(List<string> batches, StringBuilder batch)
    {
        if (!string.IsNullOrWhiteSpace(batch.ToString()))
        {
            batches.Add(batch.ToString());
        }

        batch.Clear();
    }

    // Returns the state at the end of the line. An escaped '' or ]] leaves and re-enters the state, which ends the same.
    private static State Scan(string line, State state)
    {
        for (var i = 0; i < line.Length; i++)
        {
            var next = i + 1 < line.Length ? line[i + 1] : '\0';
            switch (state, line[i])
            {
                case (State.Normal, '-') when next == '-':
                    return state;
                case (State.Normal, '\''):
                    state = State.InString;
                    break;
                case (State.Normal, '['):
                    state = State.InBrackets;
                    break;
                case (State.Normal, '"'):
                    state = State.InDoubleQuotes;
                    break;
                case (State.Normal, '/') when next == '*':
                    state = State.InBlockComment;
                    i++;
                    break;
                case (State.InString, '\''):
                case (State.InBrackets, ']'):
                case (State.InDoubleQuotes, '"'):
                    state = State.Normal;
                    break;
                case (State.InBlockComment, '*') when next == '/':
                    state = State.Normal;
                    i++;
                    break;
            }
        }

        return state;
    }
}
