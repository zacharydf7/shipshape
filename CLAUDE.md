# Shipshape

## Clean-room rule

The idea for this package came from the maintainer's employer's product. Nothing from that product or that company
may ever enter this repo:

- No code from it, and no logic ported or adapted from it.
- None of its type, file, project, database or environment-variable names. None of its SQL scripts, test data or
  ticket keys.
- No mention of the product, the company or their other repositories by name.

This covers `src/`, `tests/`, `spike/`, `README.md`, this file, commit messages, issues and package metadata.

- Write from public sources only: EF Core's source and docs, and SQL Server's docs. The schema snapshot is written fresh
  from SQL Server's catalog-view docs.
- Spikes and tests use toy `DbContext`s and migrations invented in this repo, with neutral names.
- Cross-checks against the employer's product run outside this repo. Their results go only in the private design spec,
  never here.

## What it is

Keeps EF Core's idempotent SQL Server deployment scripts generated, current and proven, for teams that hand `.sql`
files to an installer or a DBA. One test regenerates each checked-in script with its header (approval style: set
`SHIPSHAPE_ACCEPT=1` to rewrite the file). A second tier applies the script and EF's migrations to two throwaway
databases and proves they build the same schema, and that a second run changes nothing.

## Layout

- `src/Shipshape`: the package. Public: `MigrationScript` (`For`, `VerifyFileAsync`, `VerifyAgainstServerAsync`),
  `MigrationScriptOptions`, `ScriptHeader`, `ScriptCheck`, `MigrationScriptException`, `WrapCustomSqlInExec()` and
  `migrationBuilder.SqlExec(...)`. Everything else is internal.
- `ServerChecks` runs the second tier; `SchemaSnapshot` describes a database from SQL Server's catalog views as sorted
  lines of text. Changing what a snapshot line says changes every consumer's failure messages, so call it out.
- `tests/Shipshape.Tests.Toys`: toy contexts with hand-written migrations, shared by both test projects.
  `tests/Scripts` holds their checked-in scripts, which the tests verify as a consumer would.
- `tests/Shipshape.Tests.Unit`: no database. `DotnetEf/*.sql` is `dotnet ef migrations script --idempotent` output
  for the toys; regenerate it with `dotnet ef migrations script --idempotent --no-build --project
  tests/Shipshape.Tests.Toys --context <Name>Context --output tests/Shipshape.Tests.Unit/DotnetEf/<Name>.sql`.
- `tests/Shipshape.Tests.SqlServer`: everything that needs SQL Server.
- The feasibility spikes that preceded v0.1 are in the history at commit e3ad95c.

## Build and test

```
dotnet build
dotnet test -c Release
```

- Tests run on Microsoft.Testing.Platform (xUnit v3): filter with `--filter-class` and `--filter-method`, not VSTest's
  `--filter`.
- Tests that need SQL Server read its connection string from `SHIPSHAPE_SQLSERVER` and are skipped without it. Locally,
  use LocalDB: `Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true`.
- Warnings are errors.
- CI tests every push and pull request on Windows and Linux, and runs the SQL Server tests on Linux against a SQL Server
  service container. Pushing a `v*` tag publishes to nuget.org; only the maintainer creates tags.

## Tests

- Name tests `Method_Should<Expectation>_When<Condition>`. Separate arrange, act and assert with blank lines, not
  comments.
- Build test inputs in code: toy contexts and hand-written migrations. Never check in real-world scripts or databases.
- After changing a toy migration, regenerate its `DotnetEf` golden, then run the unit tests with `SHIPSHAPE_ACCEPT=1`
  to update `tests/Scripts`.

## Code style

- Write for an average developer: small focused classes, familiar idioms (object initializers, plain loops), and at
  most a one-line comment explaining why.
- Make every option discoverable through an enum or static class, so users don't need to know SQL Server internals.
- Use American English. Fill lines to about 120 characters instead of putting one parameter per line.
- Keep `README.md` self-contained, and update it when the public API or what the snapshot compares changes.
