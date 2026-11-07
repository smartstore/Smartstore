using System;
using System.IO;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Smartstore.Core.Data;
using Smartstore.Core.Migrations;

namespace Smartstore.Core.Tests.Platform.Identity;

/// <summary>
/// Only this migration is scanned in the isolated test assembly. The actual
/// upstream migration's Up/Down methods are inherited without modification.
/// </summary>
public class TestCustomerUsernameUniqueIndexMigration : CustomerUsernameUniqueIndex
{
    public TestCustomerUsernameUniqueIndexMigration(SmartDbContext db)
        : base(db)
    {
    }
}

[TestFixture]
[NonParallelizable]
public class CustomerUsernameMigrationRunnerTests : ServiceTestBase
{
    private const string OriginalIndex = "IX_Customer_Username";

    [Test]
    public void Real_sqlite_upgrade_enforces_uniqueness_preserves_null_guests_and_can_downgrade()
    {
        WithTestDatabase((connectionString, file) =>
        {
            SeedCustomerTable(connectionString);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                Insert(connection, "alice", false);
                Insert(connection, "bob", false);
                Insert(connection, null, false);
                Insert(connection, null, true);
            }

            using (var runner = CreateRunner(connectionString))
            {
                runner.GetRequiredService<IMigrationRunner>().MigrateUp();
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(GetIndexIsUnique(connection), Is.True);
                    Assert.That(CountCustomers(connection), Is.EqualTo(4));
                }

                Assert.Throws<SqliteException>(() => Insert(connection, "alice", false));
                Insert(connection, "carol", false);
                Insert(connection, null, false);
                Assert.That(CountCustomers(connection), Is.EqualTo(6));
            }

            using (var runner = CreateRunner(connectionString))
            {
                runner.GetRequiredService<IMigrationRunner>().MigrateDown(0);
            }

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                Assert.That(GetIndexIsUnique(connection), Is.False);
                Insert(connection, "alice", false);
                Assert.That(CountCustomers(connection), Is.EqualTo(7));
            }
        });
    }

    [Test]
    public void Real_sqlite_upgrade_with_duplicate_usernames_fails_preflight_without_dropping_index_or_changing_data()
    {
        WithTestDatabase((connectionString, file) =>
        {
            SeedCustomerTable(connectionString);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                Insert(connection, "reserved", deleted: true);
                Insert(connection, "reserved", deleted: false);
                Insert(connection, null, deleted: false);
            }

            using (var provider = CreateRunner(connectionString))
            {
                var ex = Assert.Catch<Exception>(() =>
                    provider.GetRequiredService<IMigrationRunner>().MigrateUp());
                Assert.That(ex.ToString(), Does.Contain("duplicate non-null username"));
                Assert.That(ex.ToString(), Does.Contain("No usernames have been changed"));
            }

            using var verifyConnection = new SqliteConnection(connectionString);
            verifyConnection.Open();
            Assert.That(GetIndexIsUnique(verifyConnection), Is.False, "Original non-unique index must remain.");
            Assert.That(CountCustomers(verifyConnection), Is.EqualTo(3));
            Assert.That(CountWhere(verifyConnection, "Username = 'reserved'"), Is.EqualTo(2));
        });
    }

    [Test]
    public void Real_sqlite_upgrade_with_case_insensitive_collation_fails_on_case_variant_duplicate()
    {
        WithTestDatabase((connectionString, file) =>
        {
            SeedCustomerTable(connectionString, collation: "NOCASE");
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                Insert(connection, "Alice", deleted: false);
                Insert(connection, "alice", deleted: false);
            }

            using (var provider = CreateRunner(connectionString))
            {
                var ex = Assert.Catch<Exception>(() =>
                    provider.GetRequiredService<IMigrationRunner>().MigrateUp());
                Assert.That(ex.ToString(), Does.Contain("duplicate non-null username"));
            }

            using var verifyConnection = new SqliteConnection(connectionString);
            verifyConnection.Open();
            Assert.That(GetIndexIsUnique(verifyConnection), Is.False);
            Assert.That(CountCustomers(verifyConnection), Is.EqualTo(2));
        });
    }

    [Test]
    public void Real_sqlite_upgrade_recreates_missing_legacy_index()
    {
        WithTestDatabase((connectionString, file) =>
        {
            SeedCustomerTable(connectionString);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"DROP INDEX \"{OriginalIndex}\"";
                command.ExecuteNonQuery();
                Insert(connection, "missing-index", deleted: false);
            }

            using (var runner = CreateRunner(connectionString))
            {
                runner.GetRequiredService<IMigrationRunner>().MigrateUp();
            }

            using var verify = new SqliteConnection(connectionString);
            verify.Open();
            Assert.That(GetIndexIsUnique(verify), Is.True);
            Assert.Throws<SqliteException>(() => Insert(verify, "missing-index", deleted: false));
        });
    }

    [Test]
    public void Real_sqlite_upgrade_on_already_unique_index_is_idempotent_for_data()
    {
        WithTestDatabase((connectionString, file) =>
        {
            SeedCustomerTable(connectionString);
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $"DROP INDEX \"{OriginalIndex}\"; CREATE UNIQUE INDEX \"{OriginalIndex}\" ON \"Customer\" (\"Username\");";
                command.ExecuteNonQuery();
                Insert(connection, "already-unique", deleted: false);
                Insert(connection, null, deleted: false);
            }

            using (var runner = CreateRunner(connectionString))
            {
                runner.GetRequiredService<IMigrationRunner>().MigrateUp();
            }

            using var verify = new SqliteConnection(connectionString);
            verify.Open();
            Assert.That(GetIndexIsUnique(verify), Is.True);
            Assert.That(CountCustomers(verify), Is.EqualTo(2));
        });
    }

    private void WithTestDatabase(Action<string, string> action)
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "username-migrations");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, $"username-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={file};Pooling=False";

        try
        {
            action(connectionString, file);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private ServiceProvider CreateRunner(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<SmartDbContext>(_ => new SmartDbContext(
            new DbContextOptionsBuilder<SmartDbContext>()
                .UseInMemoryDatabase("username-index-runner-context")
                .Options));
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder
                .AddSQLite()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(TestCustomerUsernameUniqueIndexMigration).Assembly).For.Migrations());

        return services.BuildServiceProvider();
    }

    private static void SeedCustomerTable(string connectionString, string collation = "BINARY")
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE "Customer" (
                "Id" INTEGER PRIMARY KEY,
                "Username" TEXT COLLATE {collation} NULL,
                "Deleted" INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX "{OriginalIndex}" ON "Customer" ("Username");
            """;
        command.ExecuteNonQuery();
    }

    private static void Insert(SqliteConnection connection, string username, bool deleted)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO \"Customer\" (\"Username\", \"Deleted\") VALUES ($username, $deleted)";
        command.Parameters.AddWithValue("$username", (object)username ?? DBNull.Value);
        command.Parameters.AddWithValue("$deleted", deleted ? 1 : 0);
        command.ExecuteNonQuery();
    }

    private static bool GetIndexIsUnique(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"unique\" FROM pragma_index_list('Customer') WHERE name = $name";
        command.Parameters.AddWithValue("$name", OriginalIndex);
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    private static int CountCustomers(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Customer\"";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int CountWhere(SqliteConnection connection, string condition)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Customer\" WHERE " + condition;
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
