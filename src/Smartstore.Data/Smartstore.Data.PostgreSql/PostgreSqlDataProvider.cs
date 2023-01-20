using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public PostgreSqlDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        private static string GetDatabaseSizeSql(string database)
            => $@"SELECT ROUND(pg_database_size('{database}') / 1024 / 1024, 1) AS sizemb";

        public override DbSystemType ProviderType => DbSystemType.PostgreSql;

        public override DataProviderFeatures Features
            => DataProviderFeatures.Backup
            | DataProviderFeatures.Restore
            | DataProviderFeatures.Shrink
            | DataProviderFeatures.ReIndex
            | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.AccessIncrement
            | DataProviderFeatures.StreamBlob
            | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.StoredProcedures
            | DataProviderFeatures.ReadSequential;

        public override bool MARSEnabled => false;

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

        public override Task<int> InsertIntoAsync(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public override bool IsTransientException(Exception ex)
        {
            throw new NotImplementedException();
        }

        public override bool IsUniquenessViolationException(DbUpdateException ex)
        {
            throw new NotImplementedException();
        }

        protected override Task<decimal> GetDatabaseSizeCore(bool async)
        {
            var sql = $@"SELECT ROUND(pg_database_size('{DatabaseName}') / 1024 / 1024, 1) AS sizemb";
            return async
                ? Database.ExecuteQueryRawAsync<decimal>(sql).FirstOrDefaultAsync().AsTask()
                : Task.FromResult(Database.ExecuteQueryRaw<decimal>(sql).FirstOrDefault());
        }

        protected override Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            throw new NotSupportedException();
        }

        protected override Task<int> ReIndexTablesCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = $"REINDEX DATABASE \"{DatabaseName}\"";
            return async
                ? Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override IList<string> TokenizeSqlScript(string sqlScript)
        {
            throw new NotSupportedException();
        }

        public override DbParameter CreateParameter()
            => new NpgsqlParameter();
    }
}
