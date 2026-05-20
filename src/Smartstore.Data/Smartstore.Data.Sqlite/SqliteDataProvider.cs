using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Providers;

namespace Smartstore.Data.Sqlite;

internal class SqliteDataProvider : DataProvider
{
    private static readonly SemaphoreSlim _databaseLock = new(1, 1);
    
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

    protected override async Task<long> GetDatabaseSizeCore(bool async)
    {
        var connection = Database.GetDbConnection();
        var builder = new SqliteConnectionStringBuilder(connection.ConnectionString);

        // If it is not in-memory, retrieve the physical file size
        if (!string.IsNullOrEmpty(builder.DataSource) && File.Exists(builder.DataSource))
        {
            return new FileInfo(builder.DataSource).Length;
        }

        // Contingency plan: Calculate using SQL
        var sql = "SELECT page_count * page_size FROM pragma_page_count(), pragma_page_size()";
        return async
            ? await Database.ExecuteScalarRawAsync<long>(sql)
            : Database.ExecuteScalarRaw<long>(sql);
    }

    protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
    {
        // Prevent other maintenance or write operations from taking place during maintenance operations
        await _databaseLock.WaitAsync(cancelToken);
        try
        {
            const string sql = "REINDEX;";
            if (async) await Database.ExecuteSqlRawAsync(sql, cancelToken);
            else Database.ExecuteSqlRaw(sql);

            // Shrink (VACUUM) already performs its own locking, but an external lock provides additional security
            return await ShrinkDatabaseCore(async, cancelToken);
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    protected override async Task<int> OptimizeTableCore(string tableName, bool async, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(tableName, nameof(tableName));

        await _databaseLock.WaitAsync(cancelToken);
        try
        {
            var sql = $"REINDEX {EncloseIdentifier(tableName)};";

            if (async) await Database.ExecuteSqlRawAsync(sql, cancelToken);
            else Database.ExecuteSqlRaw(sql);

            return await ShrinkDatabaseCore(async, cancelToken);
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    protected override async Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
    {
        // The database is locked during the VACUUM operation in SQLite. 
        // Creating checkpoints in WAL mode to clear the logs improves performance.
        var sql = @"
        PRAGMA wal_checkpoint(TRUNCATE);
        VACUUM;
        PRAGMA optimize;";

        return async
            ? await Database.ExecuteSqlRawAsync(sql, cancelToken)
            : Database.ExecuteSqlRaw(sql);
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
        Guard.NotEmpty(backupFullPath, nameof(backupFullPath));

        await _databaseLock.WaitAsync(cancelToken);

        try
        {
            var destinationConnection = Database.GetDbConnection() as SqliteConnection;

            using var sourceConnection = new SqliteConnection($"Data Source={backupFullPath}");

            if (async)
            {
                await sourceConnection.OpenAsync(cancelToken);

                if (destinationConnection.State != System.Data.ConnectionState.Open)
                    await destinationConnection.OpenAsync(cancelToken);
            }
            else
            {
                sourceConnection.Open();
                if (destinationConnection.State != System.Data.ConnectionState.Open)
                    destinationConnection.Open();
            }

            // SQLite Online Backup API: Copies the data from the source to the destination.
            // The destination database is locked during this operation.
            sourceConnection.BackupDatabase(destinationConnection);

            return 1;
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    protected override async Task<int> BackupDatabaseCore(string fullPath, bool async, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(fullPath, nameof(fullPath));

        await _databaseLock.WaitAsync(cancelToken);

        try
        {
            var sourceConnection = Database.GetDbConnection() as SqliteConnection;
            using var destinationConnection = new SqliteConnection($"Data Source={fullPath}");

            if (async)
            {
                if (sourceConnection.State != System.Data.ConnectionState.Open)
                    await sourceConnection.OpenAsync(cancelToken);
                await destinationConnection.OpenAsync(cancelToken);
            }
            else
            {
                if (sourceConnection.State != System.Data.ConnectionState.Open)
                    sourceConnection.Open();
                destinationConnection.Open();
            }

            sourceConnection.BackupDatabase(destinationConnection);

            return 1;
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
    {
        return new SqlBlobStream(this, tableName, blobColumnName, pkColumnName, pkColumnValue);
    }
}
