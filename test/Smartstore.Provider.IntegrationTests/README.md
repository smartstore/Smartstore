# Smartstore #1508 — real database integration tests

This separate NUnit project exercises the **production** `DefaultDbHookRegistry` and `DefaultDbHookProcessor` against **actual EF Core relational SaveChanges** calls.

It covers all four Smartstore-supported database providers using a single set of 40 tests:

- SQLite (private in-memory database; no configuration)
- PostgreSQL 16 (live database required)
- MySQL 8.4 (live database required)
- SQL Server 2022 (live database required)

For each provider, the matrix checks `Added`, `Modified`, and `Deleted` saves with both matching and deliberately nonmatching DbContext types (pre-save and post-save), the shared hook registry's cache in both context access orders, inherited context types, and a real transaction rollback.

## Run

Use **disposable QA-only databases**. Do not point these tests at production, staging, or a database containing user data: the suite creates its test table and deletes test rows to isolate each scenario.

Set these environment variables to separately provisioned, disposable test databases:

```bash
export SS1508_POSTGRESQL_CONNECTION='Host=127.0.0.1;Port=35432;Database=ss1508qa;Username=<qa-user>;Password=<qa-secret>'
export SS1508_MYSQL_CONNECTION='Server=127.0.0.1;Port=33306;Database=ss1508qa;User ID=<qa-user>;Password=<qa-secret>'
export SS1508_SQLSERVER_CONNECTION='Server=127.0.0.1,11433;Database=ss1508qa;User Id=<qa-user>;Password=<qa-secret>;Encrypt=False;TrustServerCertificate=True'
dotnet test test/Smartstore.Provider.IntegrationTests/Smartstore.Provider.IntegrationTests.csproj -c Release
```

For an offline/default run without environment variables, only the SQLite cases run and the external provider cases are explicitly skipped (not silently reported as passed). This project is intentionally **separate from the default solution test suites** so repository CI does not require three running DB servers or database secrets.

## Scope and limitations

- These tests use real database connections, EF Core transactions and persistence, not mocks or an in-memory EF provider.
- They explicitly call the real registry/processor before and after actual `DbContext.SaveChangesAsync` calls. They **do not** exercise the application's full dependency-injected `HookingDbContext`/EngineContext lifecycle. Do not mistake these test results for verification of the complete application.
- No SQL/schema changes are part of the production fix; these tests create an isolated test table in QA databases.
- No claims are made about line/branch coverage, CI acceptance, or tests against other database versions.
- Close and remove disposable Docker containers after completion.
