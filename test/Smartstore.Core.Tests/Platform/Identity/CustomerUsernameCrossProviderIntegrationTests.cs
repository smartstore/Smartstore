using System;
using System.Linq;
using System.Data;
using System.Data.Common;
using FluentMigrator.Runner;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using NUnit.Framework;
using Smartstore.Core.Data;
using Smartstore.Core.Migrations;
using Smartstore.Data;
using Smartstore.Data.Providers;
using Smartstore.Test.Common;

namespace Smartstore.Core.Tests.Platform.Identity;

/// <summary>
/// Optional actual-database integration tests. Requires an isolated, disposable
/// qa1182_customer database on localhost; never use a production database.
/// Skips an unavailable provider rather than treating it as tested.
/// </summary>
[TestFixture]
[NonParallelizable]
public class CustomerUsernameCrossProviderIntegrationTests : ServiceTestBase
{
    [TestCase("postgres")]
    [TestCase("mysql")]
    [TestCase("sqlserver")]
    public void Upgrade_prevents_duplicates_allows_guests_and_can_downgrade(string engine)
    {
        WithProvider(engine, connectionString =>
        {
            ResetSchema(engine, connectionString);
            try
            {
                using (var connection = Open(engine, connectionString))
                {
                    Insert(engine, connection, "alice", false);
                    Insert(engine, connection, "bob", true);
                    Insert(engine, connection, null, false);
                    Insert(engine, connection, null, false);
                }

                using (var runner = CreateRunner(engine, connectionString))
                {
                    runner.GetRequiredService<IMigrationRunner>().MigrateUp();
                }

                using (var connection = Open(engine, connectionString))
                {
                    Assert.That(IsUnique(engine, connection), Is.True);
                    if (engine == "sqlserver")
                    {
                        Assert.That(SqlServerFilter(connection), Does.Contain("IS NOT NULL"));
                    }

                    Assert.That(CustomerCount(engine, connection), Is.EqualTo(4));
                    Assert.Catch<DbException>(() => Insert(engine, connection, "alice", false));
                    Insert(engine, connection, null, false);
                    Insert(engine, connection, "charlie", false);
                    Assert.That(CustomerCount(engine, connection), Is.EqualTo(6));
                }

                using (var runner = CreateRunner(engine, connectionString))
                {
                    runner.GetRequiredService<IMigrationRunner>().MigrateDown(0);
                }

                using (var connection = Open(engine, connectionString))
                {
                    Assert.That(IsUnique(engine, connection), Is.False);
                    Insert(engine, connection, "alice", false);
                    Assert.That(CustomerCount(engine, connection), Is.EqualTo(7));
                }
            }
            finally { ResetSchema(engine, connectionString, create: false); }
        });
    }

    [TestCase("postgres")]
    [TestCase("mysql")]
    [TestCase("sqlserver")]
    public void Upgrade_refuses_legacy_duplicates_without_data_loss(string engine)
    {
        WithProvider(engine, connectionString =>
        {
            ResetSchema(engine, connectionString);
            try
            {
                using (var connection = Open(engine, connectionString))
                {
                    Insert(engine, connection, "reserved", true);
                    Insert(engine, connection, "reserved", false);
                    Insert(engine, connection, null, false);
                }

                using (var runner = CreateRunner(engine, connectionString))
                {
                    var ex = Assert.Catch<Exception>(
                        () => runner.GetRequiredService<IMigrationRunner>().MigrateUp());
                    Assert.That(ex.ToString(), Does.Contain("duplicate non-null username"));
                    Assert.That(ex.ToString(), Does.Contain("No usernames have been changed"));
                }

                using (var connection = Open(engine, connectionString))
                {
                    Assert.That(IsUnique(engine, connection), Is.False);
                    Assert.That(CustomerCount(engine, connection), Is.EqualTo(3));
                    Assert.That(CountName(engine, connection, "reserved"), Is.EqualTo(2));
                }
            }
            finally { ResetSchema(engine, connectionString, create: false); }
        });
    }

    [TestCase("postgres")]
    [TestCase("mysql")]
    [TestCase("sqlserver")]
    public void Two_concurrent_connections_cannot_register_the_same_username(string engine)
    {
        WithProvider(engine, connectionString =>
        {
            ResetSchema(engine, connectionString);
            try
            {
                using (var runner = CreateRunner(engine, connectionString))
                    runner.GetRequiredService<IMigrationRunner>().MigrateUp();

                using var ready = new System.Threading.CountdownEvent(2);
                using var start = new System.Threading.ManualResetEventSlim();
                System.Threading.Tasks.Task<bool> Attempt() => System.Threading.Tasks.Task.Run(() =>
                {
                    using var connection = Open(engine, connectionString);
                    ready.Signal();
                    if (!start.Wait(TimeSpan.FromSeconds(15)))
                        throw new TimeoutException("Concurrent QA start signal timed out.");
                    try
                    {
                        Insert(engine, connection, "race-user", false);
                        return true;
                    }
                    catch (DbException)
                    {
                        return false;
                    }
                });

                var first = Attempt();
                var second = Attempt();
                Assert.That(ready.Wait(TimeSpan.FromSeconds(15)), Is.True);
                start.Set();
                System.Threading.Tasks.Task.WaitAll(first, second);
                Assert.That(new[] { first.Result, second.Result }.Count(x => x), Is.EqualTo(1),
                    "Exactly one simultaneous registration may own a username.");
                using var verify = Open(engine, connectionString);
                Assert.That(CountName(engine, verify, "race-user"), Is.EqualTo(1));
            }
            finally { ResetSchema(engine, connectionString, create: false); }
        });
    }

    [TestCase("postgres")]
    [TestCase("mysql")]
    public void Preflight_respects_case_insensitive_column_collation(string engine)
    {
        WithProvider(engine, connectionString =>
        {
            ResetSchema(engine, connectionString);
            try
            {
                using (var connection = Open(engine, connectionString))
                {
                    if (engine == "postgres")
                    {
                        Exec(connection, "CREATE EXTENSION IF NOT EXISTS citext;");
                        Exec(connection, "ALTER TABLE \"Customer\" ALTER COLUMN \"Username\" TYPE citext;");
                    }

                    Insert(engine, connection, "Alice", false);
                    Insert(engine, connection, "alice", false);
                }

                using (var runner = CreateRunner(engine, connectionString))
                {
                    var ex = Assert.Catch<Exception>(
                        () => runner.GetRequiredService<IMigrationRunner>().MigrateUp());
                    Assert.That(ex.ToString(), Does.Contain("duplicate non-null username"));
                }

                using var verify = Open(engine, connectionString);
                Assert.That(IsUnique(engine, verify), Is.False);
                Assert.That(CustomerCount(engine, verify), Is.EqualTo(2));
                var table = Identifier(engine, "Customer");
                var name = Identifier(engine, "Username");
                var groups = Scalar(verify, $"SELECT COUNT(*) FROM (SELECT {name} FROM {table} GROUP BY {name} HAVING COUNT(*) > 1) AS Conflicts");
                Assert.That(Convert.ToInt32(groups), Is.EqualTo(1),
                    "Database collation should group case variants into one conflicting username group.");
            }
            finally { ResetSchema(engine, connectionString, create: false); }
        });
    }

    [TestCase("postgres")]
    [TestCase("mysql")]
    [TestCase("sqlserver")]
    public void Missing_legacy_index_is_created_safely(string engine)
    {
        WithProvider(engine, connectionString =>
        {
            ResetSchema(engine, connectionString);
            try
            {
                using (var connection = Open(engine, connectionString))
                {
                    Exec(connection, engine switch
                    {
                        "sqlserver" => "DROP INDEX [IX_Customer_Username] ON [Customer];",
                        "mysql" => "DROP INDEX §IX_Customer_Username§ ON §Customer§;".Replace('§', '\u0060'),
                        _ => "DROP INDEX \"IX_Customer_Username\";"
                    });
                    Insert(engine, connection, "existing", false);
                }
                using (var runner = CreateRunner(engine, connectionString))
                    runner.GetRequiredService<IMigrationRunner>().MigrateUp();

                using var verify = Open(engine, connectionString);
                Assert.That(IsUnique(engine, verify), Is.True);
                Assert.That(CustomerCount(engine, verify), Is.EqualTo(1));
                Assert.Catch<DbException>(() => Insert(engine, verify, "existing", false));
            }
            finally { ResetSchema(engine, connectionString, create: false); }
        });
    }

    private static void WithProvider(string engine, Action<string> action)
    {
        var envName = "SMARTSTORE_USERNAME_QA_" + engine.ToUpperInvariant();
        var connectionString = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Ignore("No isolated QA database: " + envName);

        // Parse, don't substring-search: a production database name must never
        // pass this guard because a password or other field mentions the QA name.
        string actualDatabase, actualHost;
        switch (engine)
        {
            case "postgres":
                var pg = new NpgsqlConnectionStringBuilder(connectionString);
                actualDatabase = pg.Database;
                actualHost = pg.Host;
                break;
            case "mysql":
                var mysql = new MySqlConnectionStringBuilder(connectionString);
                actualDatabase = mysql.Database;
                actualHost = mysql.Server;
                break;
            case "sqlserver":
                var ms = new SqlConnectionStringBuilder(connectionString);
                actualDatabase = ms.InitialCatalog;
                actualHost = ms.DataSource.Replace("tcp:", string.Empty, StringComparison.OrdinalIgnoreCase).Split(',')[0];
                break;
            default: throw new ArgumentOutOfRangeException(nameof(engine));
        }

        if (!string.Equals(actualDatabase, "qa1182_customer", StringComparison.Ordinal))
            Assert.Fail("Destructive QA operations are permitted only on the exact qa1182_customer database.");
        if (!string.Equals(actualHost, "127.0.0.1", StringComparison.Ordinal))
            Assert.Fail("Destructive QA operations are permitted only on loopback 127.0.0.1.");

        var type = engine switch
        {
            "postgres" => DbSystemType.PostgreSql,
            "mysql" => DbSystemType.MySql,
            "sqlserver" => DbSystemType.SqlServer,
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };

        var previous = DataSettings.Instance;
        DataSettings.Instance = new DataSettings
        {
            DbFactory = new QaDbFactory(type),
            ConnectionString = "Test",
            TenantName = "Default"
        };
        try { action(connectionString); }
        finally { DataSettings.Instance = previous; }
    }

    private static ServiceProvider CreateRunner(string engine, string cs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTransient<SmartDbContext>(_ => new SmartDbContext(
            new DbContextOptionsBuilder<SmartDbContext>()
                .UseInMemoryDatabase("username-cross-provider-migrations")
                .Options));
        services.AddFluentMigratorCore().ConfigureRunner(builder =>
        {
            switch (engine)
            {
                case "postgres": builder.AddPostgres(); break;
                case "mysql": builder.AddMySql5(); break;
                case "sqlserver": builder.AddSqlServer2014(); break;
                default: throw new ArgumentOutOfRangeException(nameof(engine));
            }
            builder.WithGlobalConnectionString(cs)
                .ScanIn(typeof(TestCustomerUsernameUniqueIndexMigration).Assembly).For.Migrations();
        });
        return services.BuildServiceProvider();
    }

    private static DbConnection Open(string engine, string cs)
    {
        DbConnection c = engine switch
        {
            "postgres" => new NpgsqlConnection(cs),
            "mysql" => new MySqlConnection(cs),
            "sqlserver" => new SqlConnection(cs),
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
        c.Open();
        return c;
    }

    private static void ResetSchema(string engine, string cs, bool create = true)
    {
        using var c = Open(engine, cs);
        var drop = engine switch
        {
            "sqlserver" => "IF OBJECT_ID(N'dbo.Customer', N'U') IS NOT NULL DROP TABLE [dbo].[Customer]; IF OBJECT_ID(N'dbo.VersionInfo', N'U') IS NOT NULL DROP TABLE [dbo].[VersionInfo];",
            "postgres" => "DROP TABLE IF EXISTS \"Customer\"; DROP TABLE IF EXISTS \"VersionInfo\";",
            "mysql" => "DROP TABLE IF EXISTS §Customer§; DROP TABLE IF EXISTS §VersionInfo§;",
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
        if (engine == "mysql") drop = drop.Replace('§', '\u0060');
        Exec(c, drop);
        if (!create) return;

        var ddl = engine switch
        {
            "sqlserver" => """
                CREATE TABLE [Customer] (
                    [Id] int IDENTITY(1,1) PRIMARY KEY,
                    [Username] nvarchar(500) COLLATE Latin1_General_100_CI_AS NULL,
                    [Deleted] bit NOT NULL DEFAULT 0
                );
                CREATE INDEX [IX_Customer_Username] ON [Customer] ([Username]);
                """,
            "postgres" => """
                CREATE TABLE "Customer" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "Username" varchar(500) NULL,
                    "Deleted" boolean NOT NULL DEFAULT false
                );
                CREATE INDEX "IX_Customer_Username" ON "Customer" ("Username");
                """,
            "mysql" => """
                CREATE TABLE §Customer§ (
                    §Id§ int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    §Username§ varchar(500) COLLATE utf8mb4_0900_ai_ci NULL,
                    §Deleted§ tinyint(1) NOT NULL DEFAULT 0,
                    INDEX §IX_Customer_Username§ (§Username§)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
        if (engine == "mysql") ddl = ddl.Replace('§', '\u0060');
        Exec(c, ddl);
    }

    private static void Insert(string engine, DbConnection c, string name, bool deleted)
    {
        var table = Identifier(engine, "Customer");
        var username = Identifier(engine, "Username");
        var del = Identifier(engine, "Deleted");
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"INSERT INTO {table} ({username}, {del}) VALUES (@name, @del)";
        var p = cmd.CreateParameter(); p.ParameterName = "@name";
        p.Value = (object)name ?? DBNull.Value; cmd.Parameters.Add(p);
        var q = cmd.CreateParameter(); q.ParameterName = "@del";
        q.Value = engine == "postgres" ? deleted : deleted ? 1 : 0;
        cmd.Parameters.Add(q);
        cmd.ExecuteNonQuery();
    }

    private static string Identifier(string engine, string name) => engine switch
    {
        "sqlserver" => "[" + name + "]",
        "mysql" => '\u0060' + name + '\u0060',
        _ => "\"" + name + "\""
    };

    private static int CustomerCount(string engine, DbConnection c)
        => Convert.ToInt32(Scalar(c, $"SELECT COUNT(*) FROM {Identifier(engine, "Customer")}"));

    private static int CountName(string engine, DbConnection c, string name)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {Identifier(engine, "Customer")} WHERE {Identifier(engine, "Username")} = @name";
        var p = cmd.CreateParameter(); p.ParameterName = "@name"; p.Value = name;
        cmd.Parameters.Add(p);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static bool IsUnique(string engine, DbConnection c)
    {
        var sql = engine switch
        {
            "sqlserver" => "SELECT is_unique FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Customer') AND name = 'IX_Customer_Username'",
            "postgres" => "SELECT i.indisunique FROM pg_index i JOIN pg_class x ON x.oid=i.indexrelid JOIN pg_class t ON t.oid=i.indrelid WHERE x.relname='IX_Customer_Username' AND t.relname='Customer'",
            "mysql" => "SELECT CASE WHEN NON_UNIQUE=0 THEN 1 ELSE 0 END FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='Customer' AND INDEX_NAME='IX_Customer_Username' LIMIT 1",
            _ => throw new ArgumentOutOfRangeException(nameof(engine))
        };
        var v = Scalar(c, sql);
        Assert.That(v, Is.Not.Null, $"Index is missing on {engine}.");
        return Convert.ToBoolean(v);
    }

    private static string SqlServerFilter(DbConnection c)
        => Convert.ToString(Scalar(c,
            "SELECT filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Customer') AND name='IX_Customer_Username'"));

    private static object Scalar(DbConnection c, string sql)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        return cmd.ExecuteScalar();
    }

    private static void Exec(DbConnection c, string sql)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private sealed class QaDbFactory(DbSystemType type) : TestDbFactory
    {
        public override DbSystemType DbSystem => type;
        public override DataProvider CreateDataProvider(DatabaseFacade db) => new QaProvider(db, type);
    }

    private sealed class QaProvider(DatabaseFacade db, DbSystemType type) : TestDataProvider(db)
    {
        public override DbSystemType ProviderType => type;
        public override string EncloseIdentifier(string name) => type switch
        {
            DbSystemType.SqlServer => "[" + name + "]",
            DbSystemType.MySql => '\u0060' + name + '\u0060',
            _ => "\"" + name + "\""
        };
    }
}
