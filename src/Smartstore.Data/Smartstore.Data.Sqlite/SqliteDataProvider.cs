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

namespace Smartstore.Data.Sqlite
{
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
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AsyncToArray()
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
            // TODO: Get actual file size
            var sql = $"SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size()";
            return async
                ? Database.ExecuteQueryRawAsync<long>(sql).FirstOrDefaultAsync().AsTask()
                : Task.FromResult(Database.ExecuteQueryRaw<long>(sql).FirstOrDefault());
        }

        protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            // TODO: Lock
            var sql = $"REINDEX;";
            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql, cancelToken);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
            }

            return await ShrinkDatabaseCore(async, cancelToken);
        }

        protected override async Task<int> OptimizeTableCore(string tableName, bool async, CancellationToken cancelToken = default)
        {
            // TODO: Lock
            var sql = $"REINDEX \"{tableName}\";";
            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql, cancelToken);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
            }

            return await ShrinkDatabaseCore(async, cancelToken);
        }

        protected override Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = $"VACUUM;PRAGMA wal_checkpoint=TRUNCATE;PRAGMA optimize;PRAGMA wal_autocheckpoint;";
            return async
                ? Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
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
            if (async)
            {
                await Database.CloseConnectionAsync();
            }
            else
            {
                Database.CloseConnection();
            }
            
            using var backupConnection = Database.GetDbConnection() as SqliteConnection;
            var thisConnection = new SqliteConnection("Data Source=" + backupFullPath);

            try
            {
                SqliteConnection.ClearAllPools();
                if (async)
                {
                    await thisConnection.OpenAsync(cancelToken);
                }
                else
                {
                    thisConnection.Open();
                }
                thisConnection.BackupDatabase(backupConnection);
            }
            finally
            {
                if (async)
                {
                    await backupConnection.CloseAsync();
                    await thisConnection.CloseAsync();
                }
                else
                {
                    backupConnection.Close();
                    thisConnection.Close();
                }
                SqliteConnection.ClearPool(thisConnection);
                SqliteConnection.ClearPool(backupConnection);
            }

            return 1;
        }


        protected override async Task<int> BackupDatabaseCore(string fullPath, bool async, CancellationToken cancelToken = default)
        {
            using var backupConnection = new SqliteConnection($"Data Source={fullPath}");
            var thisConnection = Database.GetDbConnection() as SqliteConnection;

            try
            {
                if (async)
                {
                    await thisConnection.OpenAsync(cancelToken);
                }
                else
                {
                    thisConnection.Open();
                }

                thisConnection.BackupDatabase(backupConnection);
            }
            finally
            {
                if (async)
                {
                    await backupConnection.CloseAsync();
                    await thisConnection.CloseAsync();
                }
                else
                {
                    backupConnection.Close();
                    thisConnection.Close();
                }

                SqliteConnection.ClearPool(backupConnection);
                SqliteConnection.ClearPool(thisConnection);
            }

            return 1;
        }

        protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            return new SqlBlobStream(this, tableName, blobColumnName, pkColumnName, pkColumnValue);
        }
    }
}
