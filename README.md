# Shipshape

Keeps EF Core's idempotent SQL Server deployment scripts generated, current and proven.

EF Core's docs recommend SQL scripts when a DBA or an installer applies your migrations, but nothing keeps those
scripts correct. Shipshape is a test library for that case:

- **The script is current.** A test regenerates each checked-in `.sql` file from your `DbContext`, header included,
  with no database. A stale script fails the test with a diff; set `SHIPSHAPE_ACCEPT=1` and run it again to rewrite
  the file.
- **The script builds the right database.** A second test applies the script and your migrations to two throwaway
  SQL Server databases, checks that they match, and checks that running the script again changes nothing.

Status: feasibility spikes. Nothing is published yet.

## License

MIT
