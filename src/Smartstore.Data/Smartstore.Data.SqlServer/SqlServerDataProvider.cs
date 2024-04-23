using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Providers;

namespace Smartstore.Data.SqlServer
{
    public class SqlServerDataProvider : DataProvider
    {
        private const long EXPRESS_EDITION_ID = -1592396055L;
        private const long EXPRESS_ADVANCED_EDITION_ID = -133711905L;

        private static long? _editionId = null;

        private readonly static ConcurrentDictionary<string, bool> _marsCache = new();

        private readonly static FrozenSet<int> _transientErrorCodes = new[]
        {
            49920, // Cannot process request. Too many operations in progress for subscription "%ld".
            49919, // Cannot process create or update request. Too many create or update operations in progress for subscription "%ld".
            41305, // The current transaction failed to commit due to a repeatable read validation failure.
            41302, // The current transaction attempted to update a record that has been updated since the transaction started.
            41301, // Dependency failure: a dependency was taken on another transaction that later failed to commit.
            10936, // Resource ID : %d. The request limit for the elastic pool is %d and has been reached.
            1205,  // Deadlock
            20     // This exception can be thrown even if the operation completed successfully, so it's safer to let the application fail.
        }.ToFrozenSet();

        private readonly static int[] _uniquenessViolationErrorCodes = new[]
        {
            2627, // Unique constraint error
            547,  // Constraint check violation
            2601  // Duplicated key row error
        };

        public SqlServerDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        private static string OptimizeDatabaseSql(string database, string tableFilter)
            => $@"
                DECLARE @TableName NVARCHAR(260), @IndexName NVARCHAR(260), @Sql NVARCHAR(MAX), @Fragmentation FLOAT;
                DECLARE IndexCursor CURSOR FOR 
                SELECT 
                    QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) AS TableName,
                    i.name AS IndexName,
                    s.avg_fragmentation_in_percent
                FROM 
                    sys.tables t
                JOIN 
                    sys.indexes i ON t.object_id = i.object_id
                CROSS APPLY 
                    sys.dm_db_index_physical_stats(DB_ID(N'{database}'), i.object_id, NULL, NULL, 'LIMITED') s
                WHERE 
                    i.type_desc <> 'HEAP' AND s.avg_fragmentation_in_percent > 5
                    {tableFilter}

                OPEN IndexCursor;
                FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF @Fragmentation > 30
                    BEGIN
                        SET @Sql = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON ' + @TableName + ' REBUILD;';
                    END
                    ELSE
                    BEGIN
                        SET @Sql = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON ' + @TableName + ' REORGANIZE;';
                    END
                    EXEC sp_executesql @Sql;
                    FETCH NEXT FROM IndexCursor INTO @TableName, @IndexName, @Fragmentation;
                END

                CLOSE IndexCursor;
                DEALLOCATE IndexCursor;
                ";

        private static string RestoreDatabaseSql(string database)
            => $@"DECLARE @ErrorMessage NVARCHAR(4000)
                 ALTER DATABASE [{database}] SET OFFLINE WITH ROLLBACK IMMEDIATE
                 BEGIN TRY
                    RESTORE DATABASE [{database}] FROM DISK = @p0 WITH REPLACE
                 END TRY
                 BEGIN CATCH
                    SET @ErrorMessage = ERROR_MESSAGE()
                 END CATCH
                 ALTER DATABASE [{database}] SET MULTI_USER WITH ROLLBACK IMMEDIATE
                 IF (@ErrorMessage is not NULL)
                 BEGIN
                    RAISERROR (@ErrorMessage, 16, 1)
                 END";

        private static string TableInfoSql()
            => @"SELECT 
                    t.NAME AS TableName,
                    p.rows AS NumRows,
                    SUM(a.total_pages) * 8192 AS TotalSpace, 
                    SUM(a.used_pages) * 8192 AS UsedSpace
                FROM 
                    sys.tables t
                INNER JOIN      
                    sys.indexes i ON t.OBJECT_ID = i.object_id
                INNER JOIN 
                    sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                INNER JOIN 
                    sys.allocation_units a ON p.partition_id = a.container_id
                WHERE 
                    t.NAME NOT LIKE 'sys%' 
                    AND t.is_ms_shipped = 0
                    AND i.OBJECT_ID > 255 
                GROUP BY 
                    t.NAME, p.Rows
                ORDER BY 
                    TotalSpace DESC;";

        public override DbSystemType ProviderType => DbSystemType.SqlServer;

        public override string ProviderFriendlyName
        {
            get => Database.ExecuteScalarRaw<string>("SELECT @@VERSION AS version_info");
        }

        public override DataProviderFeatures Features
            => DataProviderFeatures.Backup
            | DataProviderFeatures.Restore
            | DataProviderFeatures.Shrink
            | DataProviderFeatures.OptimizeDatabase
            | DataProviderFeatures.OptimizeTable
            | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.AccessIncrement
            | DataProviderFeatures.StreamBlob
            | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.StoredProcedures
            | DataProviderFeatures.ReadSequential
            | DataProviderFeatures.ReadTableInfo;

        protected override string SqlBatchTerminator
        {
            get => "GO";
        }

        public override DbParameter CreateParameter()
        {
            return new SqlParameter();
        }

        public override bool MARSEnabled
        {
            get
            {
                var enabled = _marsCache.GetOrAdd(Database.GetConnectionString(), conString => 
                {
                    var builder = new SqlConnectionStringBuilder(conString);
                    return builder.MultipleActiveResultSets;
                });

                return enabled;
            }
        }

        public override string EncloseIdentifier(string identifier)
        {
            Guard.NotEmpty(identifier, nameof(identifier));
            return identifier.EnsureStartsWith('[').EnsureEndsWith(']');
        }

        public override string ApplyPaging(string sql, int skip, int take)
        {
            Guard.NotNegative(skip);
            Guard.NotNegative(take);

            return $@"{sql}
OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
        }

        protected override ValueTask<bool> HasDatabaseCore(string databaseName, bool async)
        {
            FormattableString sql = $"SELECT database_id FROM sys.databases WHERE name = {databaseName}";
            return async 
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync() 
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<bool> HasTableCore(string tableName, bool async)
        {
            var sql = $@"SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' AND table_catalog = '{DatabaseName}' AND table_name = '{tableName}'";
            return async
                ? Database.ExecuteQueryRawAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryRaw<string>(sql).Any());
        }

        protected override ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async)
        {
            FormattableString sql = $"SELECT column_name From INFORMATION_SCHEMA.COLUMNS WHERE table_catalog = {DatabaseName} AND table_name = {tableName} And column_name = {columnName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<string[]> GetTableNamesCore(bool async)
        {
            var sql = $"SELECT table_name From INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' and table_catalog = '{DatabaseName}'";
            return async
                ? Database.ExecuteQueryRawAsync<string>(sql).AsyncToArray()
                : ValueTask.FromResult(Database.ExecuteQueryRaw<string>(sql).ToArray());
        }

        protected override Task<int> TruncateTableCore(string tableName, bool async)
        {
            var sql = $"TRUNCATE TABLE [{tableName}]";
            return async
                ? Database.ExecuteSqlRawAsync(sql)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }
        
        protected override async Task<int> InsertIntoCore(string sql, bool async, params object[] parameters)
        {
            sql += "; SELECT @@IDENTITY;";
            return async
                ? (await Database.ExecuteQueryRawAsync<decimal>(sql, parameters).FirstOrDefaultAsync()).Convert<int>()
                : Database.ExecuteQueryRaw<decimal>(sql, parameters).FirstOrDefault().Convert<int>();
        }

        public override bool IsTransientException(Exception ex)
        {
            return DetectSqlError(ex, _transientErrorCodes);
        }

        public override bool IsUniquenessViolationException(DbUpdateException updateException)
        {
            return DetectSqlError(updateException?.InnerException, _uniquenessViolationErrorCodes);
        }
        
        protected override Task<long> GetDatabaseSizeCore(bool async)
        {
            var sql = "SELECT SUM(CAST(size AS bigint) * 8192) FROM sys.database_files";
            return async
                ? Database.ExecuteScalarRawAsync<long>(sql)
                : Task.FromResult(Database.ExecuteScalarRaw<long>(sql));
        }

        protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = OptimizeDatabaseSql(DatabaseName, string.Empty);
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
            if (!string.IsNullOrEmpty(tableName) && !IsObjectNameValid(tableName))
            {
                throw new ArgumentException("Invalid table name.", nameof(tableName));
            }

            var tableNameFilter = string.IsNullOrEmpty(tableName) ? string.Empty : $"AND t.name = '{tableName}'";
            var sql = OptimizeDatabaseSql(DatabaseName, tableNameFilter);

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
            var shrinkSql = "DBCC SHRINKDATABASE(0)";
            return async
                ? Database.ExecuteSqlRawAsync(shrinkSql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(shrinkSql));
        }

        protected override async Task<int?> GetTableIncrementCore(string tableName, bool async)
        {
            var sql = $"SELECT IDENT_CURRENT('[{tableName}]')";
            return async
               ? (await Database.ExecuteScalarRawAsync<decimal?>(sql)).Convert<int?>()
               : Database.ExecuteScalarRaw<decimal?>(sql).Convert<int?>();
        }

        protected override Task SetTableIncrementCore(string tableName, int ident, bool async)
        {
            var sql = $"DBCC CHECKIDENT([{tableName}], RESEED, {ident})";
            return async
               ? Database.ExecuteSqlRawAsync(sql)
               : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override Task<int> BackupDatabaseCore(string fullPath, bool async, CancellationToken cancelToken = default)
        {
            return async 
                ? Database.ExecuteSqlRawAsync(CreateBackupSql(), new object[] { fullPath }, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(CreateBackupSql(), new object[] { fullPath }));
        }

        protected override Task<int> RestoreDatabaseCore(string backupFullPath, bool async, CancellationToken cancelToken = default)
        {
            return async
                ? Database.ExecuteSqlRawAsync(RestoreDatabaseSql(DatabaseName), new object[] { backupFullPath }, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(RestoreDatabaseSql(DatabaseName), new object[] { backupFullPath }));
        }

        protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            return new SqlBlobStream(this, tableName, blobColumnName, pkColumnName, pkColumnValue);
        }

        protected override async Task<List<DbTableInfo>> ReadTableInfosCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = TableInfoSql();
            return async
                ? await Database.ExecuteQueryRawAsync<DbTableInfo>(sql, cancelToken).ToListAsync(cancelToken)
                : Database.ExecuteQueryRaw<DbTableInfo>(sql).ToList();
        }

        private long GetSqlServerEdition()
        {
            if (!_editionId.HasValue)
            {
                try
                {
                    _editionId = Database.ExecuteQueryRaw<long>("Select SERVERPROPERTY('EditionID')").FirstOrDefault();
                }
                catch
                {
                    // Fallback to "Express" edition (entry-level, free database).
                    _editionId = EXPRESS_EDITION_ID;
                }
            }

            return _editionId.Value;
        }

        private string CreateBackupSql()
        {
            var sql = "BACKUP DATABASE [" + DatabaseName + "] TO DISK = {0} WITH FORMAT";

            // Backup compression is not supported by "Express" or "Express with Advanced Services" edition.
            // https://expressdb.io/sql-server-express-feature-comparison.html
            var editionId = GetSqlServerEdition();
            if (editionId != EXPRESS_EDITION_ID && editionId != EXPRESS_ADVANCED_EDITION_ID)
            {
                sql += ", COMPRESSION";
            }
           
            return sql;
        }

        private static bool DetectSqlError(Exception ex, ICollection<int> errorCodes)
        {
            while (ex != null)
            {
                if (ex is SqlException sqlException)
                {
                    foreach (SqlError err in sqlException.Errors)
                    {
                        if (errorCodes.Contains(err.Number))
                        {
                            return true;
                        }
                    }

                    break;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        private static bool IsObjectNameValid(string name)
        {
            // Prevent SQL injection attacks.
            return string.IsNullOrEmpty(name) || name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }
}
