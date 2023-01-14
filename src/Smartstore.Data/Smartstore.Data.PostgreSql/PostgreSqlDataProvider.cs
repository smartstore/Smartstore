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

        private static string GetTableNamesSql(string database)
            => $@"SELECT table_name From INFORMATION_SCHEMA.TABLES WHERE table_type = 'BASE TABLE' AND table_schema = 'public' AND table_schema = '{database}'";

        public override DbSystemType ProviderType => DbSystemType.PostgreSql;

        public override DataProviderFeatures Features
            => DataProviderFeatures.AccessIncrement
            | DataProviderFeatures.ReIndex
            | DataProviderFeatures.Shrink
            | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.ReadSequential
            | DataProviderFeatures.StoredProcedures;

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

        public override string[] GetTableNames()
        {
            return Database.ExecuteQueryRaw<string>(GetTableNamesSql(DatabaseName)).ToArray();
        }

        public override async Task<string[]> GetTableNamesAsync()
        {
            return await Database.ExecuteQueryRawAsync<string>(GetTableNamesSql(DatabaseName)).AsyncToArray();
        }

        public override decimal GetDatabaseSize()
        {
            return Database.ExecuteQueryRaw<decimal>(GetDatabaseSizeSql(DatabaseName)).FirstOrDefault();
        }

        public override Task<decimal> GetDatabaseSizeAsync()
        {
            return Database.ExecuteQueryRawAsync<decimal>(GetDatabaseSizeSql(DatabaseName)).FirstOrDefaultAsync().AsTask();
        }

        protected override int? GetTableIncrementCore(string tableName)
        {
            Guard.NotEmpty(tableName);
            return Database.ExecuteScalarRaw<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')").Convert<int?>();
        }

        protected override async Task<int?> GetTableIncrementCoreAsync(string tableName)
        {
            Guard.NotEmpty(tableName);
            return (await Database.ExecuteScalarRawAsync<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')")).Convert<int?>();
        }

        public override DbParameter CreateParameter()
            => new NpgsqlParameter();
    }
}
