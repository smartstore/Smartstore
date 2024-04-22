using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Smartstore.Data.Providers;

namespace Smartstore.Data.MySql
{
    public class MySqlDataProvider : DataProvider
    {
        private static string TableInfoSql(string database)
            => $@"SELECT 
	                TABLE_NAME AS TableName, 
	                TABLE_ROWS AS NumRows,
	                (DATA_LENGTH + INDEX_LENGTH) AS TotalSpace, 
	                CAST((DATA_LENGTH + INDEX_LENGTH) AS SIGNED) - CAST(DATA_FREE AS SIGNED) AS UsedSpace
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_SCHEMA = ""{database}""
                ORDER BY TotalSpace DESC";

        public MySqlDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        public override DbSystemType ProviderType => DbSystemType.MySql;

        public override string ProviderFriendlyName
        {
            get => "MySQL " + Database.ExecuteScalarRaw<string>("SELECT @@version");
        }

        public override DataProviderFeatures Features
            => DataProviderFeatures.OptimizeDatabase
            | DataProviderFeatures.OptimizeTable
            | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.AccessIncrement
            | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.StoredProcedures
            | DataProviderFeatures.ReadSequential
            | DataProviderFeatures.ReadTableInfo;

        protected override bool SqlSupportsDelimiterStatement
        {
            get => true;
        }

        public override DbParameter CreateParameter()
            => new MySqlParameter();

        public override bool MARSEnabled => false;

        public override string EncloseIdentifier(string identifier)
        {
            Guard.NotEmpty(identifier, nameof(identifier));
            return identifier.EnsureStartsWith('`').EnsureEndsWith('`');
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
            FormattableString sql = $"SELECT SCHEMA_NAME FROM information_schema.schemata WHERE SCHEMA_NAME = {databaseName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<bool> HasTableCore(string tableName, bool async)
        {
            FormattableString sql = $@"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = {DatabaseName} AND TABLE_NAME = {tableName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async)
        {
            FormattableString sql = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = {DatabaseName} AND TABLE_NAME = {tableName} And COLUMN_NAME = {columnName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<string[]> GetTableNamesCore(bool async)
        {
            FormattableString sql = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = {DatabaseName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AsyncToArray()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).ToArray());
        }

        protected override Task<int> TruncateTableCore(string tableName, bool async)
        {
            var sql = $"TRUNCATE TABLE `{tableName}`";
            return async
                ? Database.ExecuteSqlRawAsync(sql)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override async Task<int> InsertIntoCore(string sql, bool async, params object[] parameters)
        {
            sql += "; SELECT LAST_INSERT_ID();";
            return async
                ? (await Database.ExecuteQueryRawAsync<decimal>(sql, parameters).FirstOrDefaultAsync()).Convert<int>()
                : Database.ExecuteQueryRaw<decimal>(sql, parameters).FirstOrDefault().Convert<int>();
        }

        public override bool IsTransientException(Exception ex)
            => ex is MySqlException mySqlException
                ? mySqlException.IsTransient
                : ex is TimeoutException;

        public override bool IsUniquenessViolationException(DbUpdateException updateException)
        {
            if (updateException?.InnerException is MySqlException ex)
            {
                switch (ex.ErrorCode)
                {
                    case MySqlErrorCode.DuplicateEntryWithKeyName:
                    case MySqlErrorCode.DuplicateKey:
                    case MySqlErrorCode.DuplicateKeyEntry:
                    case MySqlErrorCode.DuplicateKeyName:
                    case MySqlErrorCode.DuplicateUnique:
                    case MySqlErrorCode.ForeignDuplicateKey:
                    case MySqlErrorCode.DuplicateEntryAutoIncrementCase:
                    case MySqlErrorCode.NonUnique:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }
        
        protected override Task<long> GetDatabaseSizeCore(bool async)
        {
            var sql = $@"SELECT CAST(SUM(DATA_LENGTH + INDEX_LENGTH) AS SIGNED) AS 'size'
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = '{DatabaseName}'";
            return async
                ? Database.ExecuteScalarRawAsync<long>(sql)
                : Task.FromResult(Database.ExecuteScalarRaw<long>(sql));
        }

        protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sqlTables = $"SHOW TABLES FROM `{DatabaseName}`";
            var tables = async 
                ? await Database.ExecuteQueryRawAsync<string>(sqlTables, cancelToken).ToListAsync(cancelToken)
                : Database.ExecuteQueryRaw<string>(sqlTables).ToList();
            
            if (tables.Count > 0)
            {
                var sql = $"OPTIMIZE TABLE `{string.Join("`, `", tables)}`";
                return async 
                    ? await Database.ExecuteSqlRawAsync(sql, cancelToken)
                    : Database.ExecuteSqlRaw(sql);
            }

            return 0;
        }

        protected override async Task<int> OptimizeTableCore(string tableName, bool async, CancellationToken cancelToken = default)
        {
            var sql = $"OPTIMIZE TABLE `{tableName}`";
            return async
                ? await Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Database.ExecuteSqlRaw(sql);
        }

        protected override async Task<int?> GetTableIncrementCore(string tableName, bool async)
        {
            FormattableString sql = $"SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = {DatabaseName} AND TABLE_NAME = {tableName}";
            return async
               ? (await Database.ExecuteScalarInterpolatedAsync<ulong>(sql)).Convert<int?>()
               : Database.ExecuteScalarInterpolated<ulong>(sql).Convert<int?>();
        }

        protected override Task SetTableIncrementCore(string tableName, int ident, bool async)
        {
            var sql = $"ALTER TABLE `{tableName}` AUTO_INCREMENT = {ident}";
            return async
               ? Database.ExecuteSqlRawAsync(sql)
               : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            return new SqlBlobStream(this, tableName, blobColumnName, pkColumnName, pkColumnValue);
        }

        protected override async Task<List<DbTableInfo>> ReadTableInfosCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = TableInfoSql(DatabaseName);
            return async
                ? await Database.ExecuteQueryRawAsync<DbTableInfo>(sql, cancelToken).ToListAsync(cancelToken)
                : Database.ExecuteQueryRaw<DbTableInfo>(sql).ToList();
        }
    }
}
