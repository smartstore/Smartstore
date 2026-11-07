using System;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Smartstore.Core.Migrations;

namespace Smartstore.Core.Tests.Platform.Identity;

[TestFixture]
public class CustomerUsernameUniqueMigrationTests
{
    private const string Table = "\"Customer\"";
    private const string Column = "\"Username\"";

    private static SqliteConnection CreateDatabase(string collation = "BINARY")
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE "Customer" (
                "Id" INTEGER PRIMARY KEY,
                "Username" TEXT COLLATE {collation} NULL,
                "Deleted" INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX "IX_Customer_Username" ON "Customer" ("Username");
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static void Insert(SqliteConnection connection, string username, bool deleted = false)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "Customer" ("Username", "Deleted") VALUES ($name, $deleted)
            """;
        command.Parameters.AddWithValue("$name", (object)username ?? DBNull.Value);
        command.Parameters.AddWithValue("$deleted", deleted ? 1 : 0);
        command.ExecuteNonQuery();
    }

    private static int Collisions(SqliteConnection connection, IDbTransaction transaction = null)
        => CustomerUsernameUniqueIndex.CountDuplicateUsernameGroups(connection, transaction, Table, Column);

    [Test]
    public void No_customers_has_no_duplicates()
    {
        using var connection = CreateDatabase();
        Assert.That(Collisions(connection), Is.Zero);
    }

    [Test]
    public void Any_number_of_anonymous_null_usernames_is_not_a_conflict()
    {
        using var connection = CreateDatabase();
        for (var i = 0; i < 25; i++)
        {
            Insert(connection, null);
        }
        Assert.That(Collisions(connection), Is.Zero);
        Assert.That(CountCustomers(connection), Is.EqualTo(25));
    }

    [TestCase("amyn", "amyn", 1)]
    [TestCase("AMYN", "amyn", 0)]
    [TestCase("", "", 1)]
    [TestCase("user with spaces", "user with spaces", 1)]
    [TestCase("مستخدم", "مستخدم", 1)]
    [TestCase("A=B' OR 1=1 --", "A=B' OR 1=1 --", 1)]
    [TestCase("é", "e", 0)]
    public void Preflight_detects_database_collation_duplicates(
        string first, string second, int expectedGroups)
    {
        using var connection = CreateDatabase();
        Insert(connection, first);
        Insert(connection, second);
        Assert.That(Collisions(connection), Is.EqualTo(expectedGroups));
        Assert.That(CountCustomers(connection), Is.EqualTo(2), "Preflight must not mutate identities.");
    }

    [Test]
    public void Preflight_counts_multiple_duplicate_username_groups_not_rows()
    {
        using var connection = CreateDatabase();
        Insert(connection, "alpha");
        Insert(connection, "alpha");
        Insert(connection, "alpha");
        Insert(connection, "beta");
        Insert(connection, "beta");
        Insert(connection, null);
        Insert(connection, null);
        Insert(connection, "gamma");

        Assert.That(Collisions(connection), Is.EqualTo(2));
        Assert.That(CountCustomers(connection), Is.EqualTo(8));
    }

    [Test]
    public void Soft_deleted_customer_usernames_still_count_toward_conflicts()
    {
        using var connection = CreateDatabase();
        Insert(connection, "reserved", deleted: true);
        Insert(connection, "reserved");
        Assert.That(Collisions(connection), Is.EqualTo(1));
    }

    [Test]
    public void Collation_sensitive_database_groups_case_variants_differently()
    {
        using var connection = CreateDatabase("NOCASE");
        Insert(connection, "User");
        Insert(connection, "user");

        Assert.That(Collisions(connection), Is.EqualTo(1),
            "Preflight must apply the database's actual column collation.");
    }

    [Test]
    public void Duplicate_detection_uses_current_transaction()
    {
        using var connection = CreateDatabase();
        using var transaction = connection.BeginTransaction();
        InsertInTransaction(connection, transaction, "pending");
        InsertInTransaction(connection, transaction, "pending");

        Assert.That(Collisions(connection, transaction), Is.EqualTo(1));
        transaction.Rollback();
        Assert.That(Collisions(connection), Is.Zero);
    }

    [Test]
    public void Preflight_on_large_batch_of_distinct_users_is_clear()
    {
        using var connection = CreateDatabase();
        using var transaction = connection.BeginTransaction();
        for (var i = 0; i < 200; i++)
        {
            InsertInTransaction(connection, transaction, $"user-{i:D4}");
        }
        Assert.That(Collisions(connection, transaction), Is.Zero);
        transaction.Commit();
        Assert.That(CountCustomers(connection), Is.EqualTo(200));
    }

    [Test]
    public void Unique_index_rejects_duplicate_username_insert_and_preserves_original()
    {
        using var connection = CreateDatabase();
        Insert(connection, "existing");
        Assert.That(Collisions(connection), Is.Zero);
        RebuildUniqueIndex(connection);

        Assert.Throws<SqliteException>(() => Insert(connection, "existing"));
        Assert.That(CountCustomers(connection), Is.EqualTo(1));
    }

    [Test]
    public void Unique_index_supports_multiple_guest_customers()
    {
        using var connection = CreateDatabase();
        RebuildUniqueIndex(connection);
        for (var i = 0; i < 10; i++)
        {
            Insert(connection, null);
        }
        Assert.That(CountCustomers(connection), Is.EqualTo(10));
    }

    [Test]
    public void Unique_index_rejects_username_even_if_previous_account_is_soft_deleted()
    {
        using var connection = CreateDatabase();
        Insert(connection, "softdelete", deleted: true);
        RebuildUniqueIndex(connection);
        Assert.Throws<SqliteException>(() => Insert(connection, "softdelete"));
        Assert.That(CountCustomers(connection), Is.EqualTo(1));
    }

    [Test]
    public void Database_collation_controls_case_insensitive_unique_index_behavior()
    {
        using var connection = CreateDatabase("NOCASE");
        Insert(connection, "Alice");
        RebuildUniqueIndex(connection);

        Assert.Throws<SqliteException>(() => Insert(connection, "alice"));
        Assert.That(CountCustomers(connection), Is.EqualTo(1));
    }

    [Test]
    public void Index_rebuild_fails_with_existing_duplicates_without_data_loss()
    {
        using var connection = CreateDatabase();
        Insert(connection, "keepme");
        Insert(connection, "keepme");

        Assert.That(Collisions(connection), Is.EqualTo(1));
        Assert.Throws<SqliteException>(() => RebuildUniqueIndex(connection));
        Assert.That(CountCustomers(connection), Is.EqualTo(2));
    }

    [Test]
    public void Multiple_distinct_non_ascii_names_are_allowed_under_unique_index()
    {
        using var connection = CreateDatabase();
        RebuildUniqueIndex(connection);
        foreach (var username in new[] { "أحمد", "سميّة", "José", "你好", "user-東京", "🧑‍💻" })
        {
            Insert(connection, username);
        }

        Assert.That(CountCustomers(connection), Is.EqualTo(6));
        Assert.That(Collisions(connection), Is.Zero);
    }

    private static void RebuildUniqueIndex(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            DROP INDEX "IX_Customer_Username";
            CREATE UNIQUE INDEX "IX_Customer_Username" ON "Customer" ("Username");
            """;
        command.ExecuteNonQuery();
    }

    private static void InsertInTransaction(SqliteConnection connection, SqliteTransaction transaction, string name)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO \"Customer\" (\"Username\") VALUES ($name)";
        command.Parameters.AddWithValue("$name", name);
        command.ExecuteNonQuery();
    }

    private static int CountCustomers(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"Customer\"";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
