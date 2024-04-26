using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Smartstore.Data.Providers;

namespace Smartstore.Data.PostgreSql
{
    public class PostgreSqlDataProvider : DataProvider
    {
        private static string TableInfoSql()
            => @"SELECT 
                    relname AS TableName,
                    n_live_tup AS NumRows,
                    pg_total_relation_size(relid) AS TotalSpace,
                    pg_table_size(relid) AS UsedSpace
                FROM pg_stat_user_tables
                ORDER BY pg_total_relation_size(relid) DESC;";

        public PostgreSqlDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        public override DbSystemType ProviderType => DbSystemType.PostgreSql;

        public override string ProviderFriendlyName
        {
            get => Database.ExecuteScalarRaw<string>("SELECT version()");
        }

        public override DataProviderFeatures Features
            => DataProviderFeatures.Shrink
            | DataProviderFeatures.OptimizeDatabase
            | DataProviderFeatures.OptimizeTable
            | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.AccessIncrement
            | DataProviderFeatures.StreamBlob
            | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.ReadSequential
            | DataProviderFeatures.StoredProcedures
            | DataProviderFeatures.ReadTableInfo;

        protected override bool SqlSupportsDelimiterStatement
        {
            get => true;
        }

        public override DbParameter CreateParameter()
        {
            return new NpgsqlParameter();
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
            FormattableString sql = $"SELECT catalog_name FROM information_schema.schemata WHERE schema_name = 'public' AND catalog_name = {databaseName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<bool> HasTableCore(string tableName, bool async)
        {
            FormattableString sql = $@"SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' AND table_schema = 'public' AND table_catalog = {DatabaseName} AND table_name = {tableName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async)
        {
            FormattableString sql = $@"SELECT column_name FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'public' AND table_catalog = {DatabaseName} AND table_name = {tableName} AND column_name = {columnName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AnyAsync()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).Any());
        }

        protected override ValueTask<string[]> GetTableNamesCore(bool async)
        {
            FormattableString sql = $@"SELECT table_name FROM INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' AND table_schema = 'public' AND table_catalog = {DatabaseName}";
            return async
                ? Database.ExecuteQueryInterpolatedAsync<string>(sql).AsyncToArray()
                : ValueTask.FromResult(Database.ExecuteQueryInterpolated<string>(sql).ToArray());
        }

        protected override Task<int> TruncateTableCore(string tableName, bool async)
        {
            var sql = $"TRUNCATE TABLE \"{tableName}\"";
            return async
                ? Database.ExecuteSqlRawAsync(sql)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override async Task<int> InsertIntoCore(string sql, bool async, params object[] parameters)
        {
            sql += " RETURNING \"Id\"";
            return async
                ? await Database.ExecuteQueryRawAsync<int>(sql, parameters).FirstOrDefaultAsync()
                : Database.ExecuteQueryRaw<int>(sql, parameters).FirstOrDefault();
        }

        public override bool IsTransientException(Exception ex)
            => ex is NpgsqlException npgSqlException
                ? npgSqlException.IsTransient
                : ex is TimeoutException;

        public override bool IsUniquenessViolationException(DbUpdateException updateException)
        {
            if (updateException?.InnerException is PostgresException ex)
            {
                switch (ex.SqlState)
                {
                    case PostgresErrorCodes.IntegrityConstraintViolation:
                    case PostgresErrorCodes.RestrictViolation:
                    case PostgresErrorCodes.NotNullViolation:
                    case PostgresErrorCodes.ForeignKeyViolation:
                    case PostgresErrorCodes.CheckViolation:
                    case PostgresErrorCodes.ExclusionViolation:
                    case PostgresErrorCodes.UniqueViolation:
                        return true;
                    default:
                        return false;
                }
            }

            return false;
        }

        protected override Task<long> GetDatabaseSizeCore(bool async)
        {
            var sql = $@"SELECT pg_database_size('{DatabaseName}') as sizebytes";
            return async
                ? Database.ExecuteScalarRawAsync<long>(sql)
                : Task.FromResult(Database.ExecuteScalarRaw<long>(sql));
        }

        protected override async Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = $"REINDEX DATABASE \"{DatabaseName}\"";
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
            var sql = $"REINDEX TABLE \"{tableName}\"";
            var sqlVacuum = $"VACUUM \"{tableName}\""; // "VACUUM FULL" is VERY slow

            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql, cancelToken);
                // Free physical space
                return await Database.ExecuteSqlRawAsync(sqlVacuum, cancelToken);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
                // Free physical space
                return Database.ExecuteSqlRaw(sqlVacuum);
            }
        }

        protected override Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = "VACUUM"; // "VACUUM FULL" is VERY slow
            return async
                ? Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override async Task<int?> GetTableIncrementCore(string tableName, bool async)
        {
            var seqName = await GetSequenceName(tableName, async);
            var sql = $"SELECT COALESCE(last_value + CASE WHEN is_called THEN 1 ELSE 0 END, 1) as Value FROM {seqName}";

            return async
               ? (await Database.ExecuteScalarRawAsync<long?>(sql)).Convert<int?>()
               : Database.ExecuteScalarRaw<long?>(sql).Convert<int?>();
        }

        protected override async Task SetTableIncrementCore(string tableName, int ident, bool async)
        {
            var seqName = await GetSequenceName(tableName, async);
            var sql = $"SELECT setval('{seqName}', {ident}, false)";

            if (async)
            {
                await Database.ExecuteSqlRawAsync(sql);
            }
            else
            {
                Database.ExecuteSqlRaw(sql);
            }
        }

        private Task<string> GetSequenceName(string tableName, bool async)
        {
            var sql = $"SELECT pg_get_serial_sequence('\"{tableName}\"', 'Id')";
            var seqName = async
               ? Database.ExecuteScalarRawAsync<string>(sql)
               : Task.FromResult(Database.ExecuteScalarRaw<string>(sql));

            return seqName;
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
    }
}
