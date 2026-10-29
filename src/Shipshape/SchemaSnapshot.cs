using Microsoft.Data.SqlClient;

namespace Shipshape;

/// <summary>
/// Describes a database as sorted lines of text, one per fact, from SQL Server's catalog views. Names SQL Server makes up
/// (an unnamed default or constraint) are left out, so two builds of the same schema describe the same.
/// </summary>
internal static class SchemaSnapshot
{
    // The *_desc columns of the catalog views use a fixed collation, so they are converted before being concatenated.
    private const string Schemas = """
        SELECT N'schema ' + name FROM sys.schemas WHERE schema_id > 4 AND schema_id < 16384;
        """;

    private const string Tables = """
        SELECT N'table ' + s.name + N'.' + t.name
            + CASE WHEN t.temporal_type = 2 THEN N' system-versioned, history ' + hs.name + N'.' + h.name ELSE N'' END
        FROM sys.tables t
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        LEFT JOIN sys.tables h ON h.object_id = t.history_table_id
        LEFT JOIN sys.schemas hs ON hs.schema_id = h.schema_id
        WHERE t.is_ms_shipped = 0;
        """;

    // One line per table, so a removed column reads as one difference instead of moving every column after it.
    private const string ColumnOrder = """
        SELECT N'column order ' + s.name + N'.' + t.name + N': '
            + STRING_AGG(CAST(c.name AS nvarchar(max)), N', ') WITHIN GROUP (ORDER BY c.column_id)
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE t.is_ms_shipped = 0
        GROUP BY s.name, t.name;
        """;

    private const string Columns = """
        SELECT N'column ' + s.name + N'.' + t.name + N'.' + c.name
            + N' ' + TYPE_NAME(c.user_type_id)
            + CASE
                WHEN TYPE_NAME(c.user_type_id) IN (N'nvarchar', N'nchar')
                    THEN N'(' + IIF(c.max_length = -1, N'max', CAST(c.max_length / 2 AS nvarchar(10))) + N')'
                WHEN TYPE_NAME(c.user_type_id) IN (N'varchar', N'char', N'varbinary', N'binary')
                    THEN N'(' + IIF(c.max_length = -1, N'max', CAST(c.max_length AS nvarchar(10))) + N')'
                WHEN TYPE_NAME(c.user_type_id) IN (N'decimal', N'numeric')
                    THEN N'(' + CAST(c.precision AS nvarchar(10)) + N',' + CAST(c.scale AS nvarchar(10)) + N')'
                WHEN TYPE_NAME(c.user_type_id) IN (N'datetime2', N'time', N'datetimeoffset')
                    THEN N'(' + CAST(c.scale AS nvarchar(10)) + N')'
                ELSE N'' END
            + IIF(c.is_nullable = 1, N' null', N' not null')
            + ISNULL(N' collate ' + c.collation_name, N'')
            + ISNULL(N' identity(' + CAST(ic.seed_value AS nvarchar(40)) + N',' + CAST(ic.increment_value AS nvarchar(40)) + N')', N'')
            + ISNULL(N' as ' + cc.definition + IIF(cc.is_persisted = 1, N' persisted', N''), N'')
            + ISNULL(N' default ' + dc.definition + IIF(dc.is_system_named = 0, N' named ' + dc.name, N''), N'')
            + IIF(c.generated_always_type > 0, N' ' + c.generated_always_type_desc COLLATE DATABASE_DEFAULT, N'')
            + IIF(c.is_hidden = 1, N' hidden', N'')
            + IIF(c.is_sparse = 1, N' sparse', N'')
            + IIF(c.is_rowguidcol = 1, N' rowguidcol', N'')
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        LEFT JOIN sys.identity_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
        LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
        LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE t.is_ms_shipped = 0;
        """;

    // Columnstore, XML and spatial indexes (type 3 and up) give every column a key_ordinal of 0.
    private const string Indexes = """
        SELECT N'index ' + s.name + N'.' + t.name + N' ' + IIF(kc.is_system_named = 1, N'(unnamed)', i.name)
            + N' ' + i.type_desc COLLATE DATABASE_DEFAULT
            + CASE WHEN i.is_primary_key = 1 THEN N' primary key' WHEN i.is_unique_constraint = 1 THEN N' unique constraint'
                WHEN i.is_unique = 1 THEN N' unique' ELSE N'' END
            + N' (' + keys.list + N')'
            + ISNULL(N' include (' + included.list + N')', N'')
            + ISNULL(N' where ' + i.filter_definition, N'')
            + IIF(i.is_disabled = 1, N' disabled', N'')
        FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        LEFT JOIN sys.key_constraints kc ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
        CROSS APPLY (
            SELECT STRING_AGG(c.name + IIF(ic.is_descending_key = 1, N' desc', N''), N', ')
                WITHIN GROUP (ORDER BY ic.key_ordinal, ic.index_column_id)
            FROM sys.index_columns ic
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND (ic.key_ordinal > 0 OR i.type > 2)) keys(list)
        CROSS APPLY (
            SELECT STRING_AGG(c.name, N', ') WITHIN GROUP (ORDER BY c.name)
            FROM sys.index_columns ic
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 1) included(list)
        WHERE i.type > 0 AND t.is_ms_shipped = 0;
        """;

    private const string ForeignKeys = """
        SELECT N'foreign key ' + s.name + N'.' + t.name + N' ' + IIF(fk.is_system_named = 1, N'(unnamed)', fk.name)
            + N' (' + cols.parent + N') references ' + rs.name + N'.' + rt.name + N' (' + cols.referenced + N')'
            + N' on delete ' + fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT
            + N' on update ' + fk.update_referential_action_desc COLLATE DATABASE_DEFAULT
            + IIF(fk.is_disabled = 1, N' disabled', N'')
            + IIF(fk.is_not_trusted = 1, N' not trusted', N'')
        FROM sys.foreign_keys fk
        JOIN sys.tables t ON t.object_id = fk.parent_object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
        JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
        CROSS APPLY (
            SELECT STRING_AGG(pc.name, N', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id),
                STRING_AGG(rc.name, N', ') WITHIN GROUP (ORDER BY fkc.constraint_column_id)
            FROM sys.foreign_key_columns fkc
            JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
            JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            WHERE fkc.constraint_object_id = fk.object_id) cols(parent, referenced);
        """;

    private const string CheckConstraints = """
        SELECT N'check ' + s.name + N'.' + t.name + N' ' + IIF(cc.is_system_named = 1, N'(unnamed)', cc.name)
            + N' ' + cc.definition
            + IIF(cc.is_disabled = 1, N' disabled', N'')
            + IIF(cc.is_not_trusted = 1, N' not trusted', N'')
        FROM sys.check_constraints cc
        JOIN sys.tables t ON t.object_id = cc.parent_object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id;
        """;

    private const string Sequences = """
        SELECT N'sequence ' + s.name + N'.' + q.name + N' ' + TYPE_NAME(q.user_type_id)
            + N' start ' + CAST(q.start_value AS nvarchar(40)) + N' increment ' + CAST(q.increment AS nvarchar(40))
            + N' min ' + CAST(q.minimum_value AS nvarchar(40)) + N' max ' + CAST(q.maximum_value AS nvarchar(40))
            + IIF(q.is_cycling = 1, N' cycle', N'')
            + IIF(q.is_cached = 0, N' no cache', ISNULL(N' cache ' + CAST(q.cache_size AS nvarchar(20)), N''))
        FROM sys.sequences q
        JOIN sys.schemas s ON s.schema_id = q.schema_id;
        """;

    // Views, procedures, functions and triggers, with the text they were created from.
    private const string Modules = """
        SELECT N'module ' + o.type_desc COLLATE DATABASE_DEFAULT + N' ' + s.name + N'.' + o.name
            + ISNULL(N' on ' + ps.name + N'.' + p.name, N'')
            + IIF(OBJECTPROPERTY(o.object_id, 'ExecIsTriggerDisabled') = 1, N' disabled', N'')
            + N' ' + ISNULL(m.definition, N'(encrypted)')
        FROM sys.sql_modules m
        JOIN sys.objects o ON o.object_id = m.object_id
        JOIN sys.schemas s ON s.schema_id = o.schema_id
        LEFT JOIN sys.objects p ON p.object_id = o.parent_object_id AND o.type = 'TR'
        LEFT JOIN sys.schemas ps ON ps.schema_id = p.schema_id
        WHERE o.is_ms_shipped = 0 AND o.type IN ('V', 'P', 'FN', 'IF', 'TF', 'TR');
        """;

    // Every migration ID in every history table: any table with EF's MigrationId and ProductVersion columns. The EF
    // version is left out: a script from another EF version is stale, which the file check reports first.
    private const string History = """
        DECLARE @sql nvarchar(max) = (
            SELECT STRING_AGG(CAST(N'SELECT N''history ' + REPLACE(s.name + N'.' + t.name, N'''', N'''''')
                + N' '' + [MigrationId] FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
                AS nvarchar(max)), N' UNION ALL ')
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE COL_LENGTH(QUOTENAME(s.name) + N'.' + QUOTENAME(t.name), N'MigrationId') IS NOT NULL
                AND COL_LENGTH(QUOTENAME(s.name) + N'.' + QUOTENAME(t.name), N'ProductVersion') IS NOT NULL);
        IF @sql IS NOT NULL EXEC (@sql);
        """;

    // Row counts per table, and with {0} = 1 a checksum of each table's rows. {0} is filled in before running.
    private const string Rows = """
        DECLARE @sql nvarchar(max) = (
            SELECT STRING_AGG(CAST(N'SELECT N''rows ' + REPLACE(s.name + N'.' + t.name, N'''', N'''''')
                + N' '' + CAST(COUNT_BIG(*) AS nvarchar(20))'
                + IIF({0} = 1, N' + N'' checksum '' + ISNULL(CAST(CHECKSUM_AGG(BINARY_CHECKSUM(*)) AS nvarchar(20)), N''0'')', N'')
                + N' FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) AS nvarchar(max)), N' UNION ALL ')
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0 AND t.temporal_type <> 1);
        IF @sql IS NOT NULL EXEC (@sql);
        """;

    /// <summary>
    /// Takes the snapshot. Checksums catch changed rows, but differ between databases whose rows hold different times,
    /// so they only suit comparing a database with itself.
    /// </summary>
    public static async Task<IReadOnlyList<string>> TakeAsync(string connectionString, bool includeChecksums = false)
    {
        string[] queries =
        [
            Schemas, Tables, ColumnOrder, Columns, Indexes, ForeignKeys, CheckConstraints, Sequences, Modules, History,
            string.Format(Rows, includeChecksums ? 1 : 0),
        ];

        var facts = new List<string>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var query in queries)
        {
            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                facts.Add(TrimLines(reader.GetString(0)));
            }
        }

        facts.Sort(StringComparer.Ordinal);
        return facts;
    }

    // An idempotent script indents every line of custom SQL, even inside an EXEC string, so a view or procedure it
    // creates is stored with different leading whitespace than Migrate() gives it.
    private static string TrimLines(string fact) =>
        string.Join('\n', fact.ReplaceLineEndings("\n").Split('\n').Select(line => line.Trim())).Trim();

    /// <summary>The facts only one side has: "- " for only in <paramref name="expected"/>, "+ " for only in <paramref name="actual"/>.</summary>
    public static IReadOnlyList<string> Compare(IReadOnlyList<string> expected, IReadOnlyList<string> actual) =>
    [
        .. expected.Except(actual, StringComparer.Ordinal).Select(fact => "- " + fact),
        .. actual.Except(expected, StringComparer.Ordinal).Select(fact => "+ " + fact),
    ];
}
