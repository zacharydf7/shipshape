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

The repo is at the feasibility stage: `spike/` holds the prototypes that answer the design's go/no-go questions. The
library (`src/Shipshape`) and its tests come after the gate is approved.

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

## Code style

- Write for an average developer: small focused classes, familiar idioms (object initializers, plain loops), and at
  most a one-line comment explaining why.
- Make every option discoverable through an enum or static class, so users don't need to know SQL Server internals.
- Use American English. Fill lines to about 120 characters instead of putting one parameter per line.
