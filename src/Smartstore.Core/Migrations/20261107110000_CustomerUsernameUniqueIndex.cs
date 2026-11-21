using System.Data;
using System.Globalization;
using FluentMigrator;
using FluentMigrator.SqlServer;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Data;

namespace Smartstore.Core.Migrations;

/// <summary>
/// Enforces a unique customer username index on upgraded installations.
/// Fresh installations receive the same unique index from the EF model.
/// </summary>
[MigrationVersion("2026-11-07 11:00:00", "Core: Require unique customer usernames")]
public class CustomerUsernameUniqueIndex : Migration
{
    internal const string TableName = nameof(Customer);
    internal const string ColumnName = nameof(Customer.Username);
    internal const string IndexName = "IX_Customer_Username";

    private readonly SmartDbContext _db;

    public CustomerUsernameUniqueIndex(SmartDbContext db)
    {
        _db = db;
    }

    public override void Up()
    {
        if (!Schema.Table(TableName).Exists())
        {
            return;
        }

        // Check BEFORE dropping the existing index. An upgrade with legacy
        // duplicates must stop without silently changing customer identities.
        // Include soft-deleted users: this index protects all non-null names.
        var table = _db.DataProvider.EncloseIdentifier(TableName);
        var column = _db.DataProvider.EncloseIdentifier(ColumnName);

        Execute.WithConnection((connection, transaction) =>
        {
            var duplicates = CountDuplicateUsernameGroups(connection, transaction, table, column);
            if (duplicates > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot create unique customer username index '{IndexName}': " +
                    $"{duplicates} duplicate non-null username group(s) exist (including soft-deleted records). " +
                    "Resolve the conflicts explicitly before upgrading and retry. " +
                    "No usernames have been changed by this migration.");
            }
        });

        if (Schema.Table(TableName).Index(IndexName).Exists())
        {
            Delete.Index(IndexName).OnTable(TableName);
        }

        var index = Create.Index(IndexName)
            .OnTable(TableName)
            .OnColumn(ColumnName).Ascending()
            .WithOptions().Unique();

        // SQL Server permits only one NULL in an unfiltered UNIQUE index.
        // Anonymous customers must still be allowed to have NULL usernames.
        if (_db.DataProvider.ProviderType == DbSystemType.SqlServer)
        {
            index.WithOptions().Filter($"([{ColumnName}] IS NOT NULL)");
        }
    }

    public override void Down()
    {
        if (!Schema.Table(TableName).Exists())
        {
            return;
        }

        if (Schema.Table(TableName).Index(IndexName).Exists())
        {
            Delete.Index(IndexName).OnTable(TableName);
        }

        Create.Index(IndexName)
            .OnTable(TableName)
            .OnColumn(ColumnName).Ascending();
    }

    /// <summary>
    /// Counts duplicate non-null username groups using the database collation
    /// used by its index. Identifiers come from the data provider, not users.
    /// </summary>
    internal static int CountDuplicateUsernameGroups(
        IDbConnection connection,
        IDbTransaction transaction,
        string enclosedTable,
        string enclosedColumn)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = $"""
            SELECT COUNT(*)
            FROM (
                SELECT {enclosedColumn}
                FROM {enclosedTable}
                WHERE {enclosedColumn} IS NOT NULL
                GROUP BY {enclosedColumn}
                HAVING COUNT(*) > 1
            ) AS DuplicateUsernames
            """;

        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
