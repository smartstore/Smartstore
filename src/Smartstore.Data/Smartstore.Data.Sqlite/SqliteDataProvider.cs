using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Providers;
using Smartstore.Threading;

namespace Smartstore.Data.Sqlite;

internal class SqliteDataProvider : DataProvider
{
    public SqliteDataProvider(DatabaseFacade database)
        : base(database)
    {
    }

    public override DbSystemType ProviderType => DbSystemType.SQLite;

    public override string ProviderFriendlyName
    {
        get => "SQLite " + Database.ExecuteScalarRaw<string>("SELECT sqlite_version()");
    }

    public override DataProviderFeatures Features
        => DataProviderFeatures.Backup
        | DataProviderFeatures.Restore
        | DataProviderFeatures.Shrink
        | DataProviderFeatures.OptimizeDatabase
        | DataProviderFeatures.OptimizeTable
        | DataProviderFeatures.ComputeSize
        | DataProviderFeatures.AccessIncrement
        | DataProviderFeatures.ReadSequential
        | DataProviderFeatures.ExecuteSqlScript;

    public override DbParameter CreateParameter()
    {
        return new SqliteParameter();
    }

    public override bool MARSEnabled => false;

    public override string EncloseIdentifier(string identifier)
    {
        Guard.NotEmpty(identifier, nameof(identifier));
        return identifier.EnsureStartsWith('"').EnsureEndsWith('"');
    }

    public override string ApplyPaging(string sql, int skip, int take)
    {
        Guard.NotNegative(skip);
        Guard.NotNegative(take);

        return $@"{sql}
LIMIT {take} OFFSET {skip}";
    }

    protected override ValueTask<bool> HasDatabaseCore(string databaseName, bool async)
    {
        return ValueTask.FromResult(true);
    }

    protected override ValueTask<bool> HasTableCore(string tableName, bool async)
    {
        FormattableString sql = $@"SELECT name FROM sqlite_master WHERE type = 'table' AND name = {tableName}";
        return async
            ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
            : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
    }

    protected override ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async)
    {
        FormattableString sql = $@"SELECT name FROM pragma_table_info({tableName}) WHERE name = {columnName}";
        return async
            ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
            : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
    }

    protected override ValueTask<string[]> GetTableNamesCore(bool async)
    {
        FormattableString sql = $@"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
        return async
            ? Database.ExecuteQueryInterpolatedAsync<string>(sql).ToArrayAsync()
            : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).ToArray());
    }

    protected override Task<int> TruncateTableCore(string tableName, bool async)
    {
        var sql = $"DELETE FROM \"{tableName}\"";
        return async
            ? Database.ExecuteSqlRawAsync(sql)
            : Task.FromResult(Database.ExecuteSqlRaw(sql));
    }

    protected override async Task<int> InsertIntoCore(string sql, bool async, params object[] parameters)
    {
        sql += "; SELECT last_insert_rowid();";
        return async
            ? await Database.ExecuteQueryRawAsync<int>(sql, parameters).FirstOrDefaultAsync()
            : Database.ExecuteQueryRaw<int>(sql, parameters).FirstOrDefault();
    }

    public override bool IsTransientException(Exception ex)
        => ex is SqliteException sqliteException
            ? sqliteException.IsTransient
            : ex is TimeoutException;

    public override bool IsUniquenessViolationException(DbUpdateException updateException)
    {
        if (updateException?.InnerException is SqliteException ex)
        {
            // SQLiteErrorCode.Constraint = 10
            return ex.SqliteErrorCode == 19;
        }

        return false;
    }

    protected override Task<long> GetDatabaseSizeCore(bool async)
    {
        var connection = GetSqliteConnection();
        var builder = new SqliteConnectionStringBuilder(connection.ConnectionString);

        if (builder.DataSource.HasValue() && File.Exists(builder.DataSource))
        {
            return Task.FromResult(new FileInfo(builder.DataSource).Length);
        }

        const string sql = "SELECT page_count * page_size AS size FROM pragma_page_count(), pragma_page_size()";
        return async
            ? Database.ExecuteScalarRawAsync<long>(sql)
            : Task.FromResult(Database.ExecuteScalarRaw<long>(sql));
    }

    protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
    {
        return await ExecuteMaintenanceOperationAsync(async () =>
        {
            const string sql = "REINDEX;";
            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql, cancelToken);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
            }

            return await ShrinkDatabaseInternalCore(async, cancelToken);
        }, cancelToken);
    }

    protected override async Task<int> OptimizeTableCore(string tableName, bool async, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(tableName);

        return await ExecuteMaintenanceOperationAsync(async () =>
        {
            var sql = $"REINDEX {EncloseIdentifier(tableName)};";
            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql, cancelToken);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
            }

            return await ShrinkDatabaseInternalCore(async, cancelToken);
        }, cancelToken);
    }

    protected override async Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
    {
        return await ExecuteMaintenanceOperationAsync(() => ShrinkDatabaseInternalCore(async, cancelToken), cancelToken);
    }

    protected override Task<int?> GetTableIncrementCore(string tableName, bool async)
    {
        var sql = $"SELECT seq FROM sqlite_sequence WHERE name = \"{tableName}\"";

        return async
           ? Database.ExecuteScalarRawAsync<int?>(sql)
           : Task.FromResult(Database.ExecuteScalarRaw<int?>(sql));
    }

    protected override Task SetTableIncrementCore(string tableName, int ident, bool async)
    {
        var sql = $"UPDATE sqlite_sequence SET seq = {ident} WHERE name = \"{tableName}\"";
        return async
           ? Database.ExecuteSqlRawAsync(sql)
           : Task.FromResult(Database.ExecuteSqlRaw(sql));
    }

    protected override async Task<int> RestoreDatabaseCore(string backupFullPath, bool async, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(backupFullPath);

        return await ExecuteMaintenanceOperationAsync(async () =>
        {
            var destinationConnection = GetSqliteConnection();
            using var sourceConnection = new SqliteConnection($"Data Source={backupFullPath}");

            try
            {
                if (async)
                {
                    await Database.CloseConnectionAsync();
                }
                else
                {
                    Database.CloseConnection();
                }

                SqliteConnection.ClearAllPools();

                if (async)
                {
                    await sourceConnection.OpenAsync(cancelToken);
                    await destinationConnection.OpenAsync(cancelToken);
                }
                else
                {
                    sourceConnection.Open();
                    destinationConnection.Open();
                }

                sourceConnection.BackupDatabase(destinationConnection);

                return 1;
            }
            finally
            {
                if (async)
                {
                    await Database.CloseConnectionAsync();
                    await sourceConnection.CloseAsync();
                }
                else
                {
                    Database.CloseConnection();
                    sourceConnection.Close();
                }

                SqliteConnection.ClearPool(sourceConnection);
                SqliteConnection.ClearPool(destinationConnection);
            }
        }, cancelToken);
    }


    protected override async Task<int> BackupDatabaseCore(string fullPath, bool async, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(fullPath);

        return await ExecuteMaintenanceOperationAsync(async () =>
        {
            using var destinationConnection = new SqliteConnection($"Data Source={fullPath}");
            var sourceConnection = GetSqliteConnection();
            var closeSourceConnection = false;

            try
            {
                if (async)
                {
                    if (sourceConnection.State != ConnectionState.Open)
                    {
                        await sourceConnection.OpenAsync(cancelToken);
                        closeSourceConnection = true;
                    }

                    await destinationConnection.OpenAsync(cancelToken);
                }
                else
                {
                    if (sourceConnection.State != ConnectionState.Open)
                    {
                        sourceConnection.Open();
                        closeSourceConnection = true;
                    }

                    destinationConnection.Open();
                }

                sourceConnection.BackupDatabase(destinationConnection);

                return 1;
            }
            finally
            {
                if (async)
                {
                    await destinationConnection.CloseAsync();

                    if (closeSourceConnection)
                    {
                        await sourceConnection.CloseAsync();
                    }
                }
                else
                {
                    destinationConnection.Close();

                    if (closeSourceConnection)
                    {
                        sourceConnection.Close();
                    }
                }

                SqliteConnection.ClearPool(destinationConnection);

                if (closeSourceConnection)
                {
                    SqliteConnection.ClearPool(sourceConnection);
                }
            }
        }, cancelToken);
    }

    protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
    {
        return new SqlBlobStream(this, tableName, blobColumnName, pkColumnName, pkColumnValue);
    }

    private SqliteConnection GetSqliteConnection()
        => Database.GetDbConnection() as SqliteConnection ?? throw new InvalidOperationException("Expected a SQLite connection.");

    private string GetMaintenanceLockKey()
    {
        var connection = GetSqliteConnection();
        var builder = new SqliteConnectionStringBuilder(connection.ConnectionString);
        return builder.DataSource.NullEmpty() ?? connection.ConnectionString;
    }

    private async Task<T> ExecuteMaintenanceOperationAsync<T>(Func<Task<T>> action, CancellationToken cancelToken)
    {
        var key = GetMaintenanceLockKey();
        await using var _ = await AsyncLock.KeyedAsync(key, cancelToken: cancelToken);
        return await action();
    }

    private async Task<int> ShrinkDatabaseInternalCore(bool async, CancellationToken cancelToken = default)
    {
        const string checkpointSql = "PRAGMA wal_checkpoint(TRUNCATE);";
        const string vacuumSql = "VACUUM;";
        const string optimizeSql = "PRAGMA optimize;";

        if (async)
        {
            await Database.ExecuteSqlRawAsync(checkpointSql, cancelToken);
            await Database.ExecuteSqlRawAsync(vacuumSql, cancelToken);
            return await Database.ExecuteSqlRawAsync(optimizeSql, cancelToken);
        }

        Database.ExecuteSqlRaw(checkpointSql);
        Database.ExecuteSqlRaw(vacuumSql);
        return Database.ExecuteSqlRaw(optimizeSql);
    }

    }
