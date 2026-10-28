using System.Text;

namespace Shipshape.Spike;

/// <summary>The statements a script starts with, before EF's migrations. They are written in this order.</summary>
[Flags]
public enum ScriptHeader
{
    None = 0,

    /// <summary><c>SET QUOTED_IDENTIFIER ON</c>: sqlcmd turns it off, and filtered indexes and computed columns need it.</summary>
    QuotedIdentifierOn = 1,

    /// <summary>Creates the database when it doesn't exist yet.</summary>
    CreateDatabaseIfMissing = 2,

    /// <summary><c>USE</c> the database, so the script runs against it from any connection.</summary>
    UseDatabase = 4,
}

public static class ScriptHeaderWriter
{
    public static string Write(ScriptHeader header, string databaseName)
    {
        var name = "[" + databaseName.Replace("]", "]]") + "]";
        var text = new StringBuilder();
        if (header.HasFlag(ScriptHeader.QuotedIdentifierOn))
        {
            text.Append("SET QUOTED_IDENTIFIER ON;\nGO\n\n");
        }

        if (header.HasFlag(ScriptHeader.CreateDatabaseIfMissing))
        {
            text.Append($"IF DB_ID(N'{databaseName.Replace("'", "''")}') IS NULL\n    CREATE DATABASE {name};\nGO\n\n");
        }

        if (header.HasFlag(ScriptHeader.UseDatabase))
        {
            text.Append($"USE {name};\nGO\n\n");
        }

        return text.ToString();
    }
}
