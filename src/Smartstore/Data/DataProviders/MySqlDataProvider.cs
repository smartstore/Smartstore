using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Dasync.Collections;
using System.Text.RegularExpressions;
using System.IO;

namespace Smartstore.Data.DataProviders
{
    public class MySqlDataProvider : DataProvider
    {
        internal sealed class MySqlTableSchema
        {
            public string Database { get; set; }
            public decimal SizeMB { get; set; }
        }
        
        public MySqlDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        private string GetDatabaseSizeSql(string database)
            => $@"SELECT table_schema AS 'Database', ROUND(SUM(data_length + index_length) / 1024 / 1024, 1) AS 'SizeMB' 
                FROM information_schema.TABLES
                WHERE table_schema = '{database}'
                GROUP BY table_schema";

        public override DataProviderFeatures Features 
            => DataProviderFeatures.AccessIdent | DataProviderFeatures.ReIndex | DataProviderFeatures.Shrink | DataProviderFeatures.ComputeSize
            | DataProviderFeatures.ExecuteSqlScript | DataProviderFeatures.StreamBlob;

        protected override int? GetTableIdentCore(string tableName)
        {
            return Database.ExecuteScalarInterpolated<decimal?>(
                $"SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = {Database.GetDbConnection().Database} AND TABLE_NAME = {tableName}").Convert<int?>();
        }

        protected override async Task<int?> GetTableIdentCoreAsync(string tableName)
        {
            return (await Database.ExecuteScalarInterpolatedAsync<decimal?>(
                $"SELECT AUTO_INCREMENT FROM information_schema.TABLES WHERE TABLE_SCHEMA = {Database.GetDbConnection().Database} AND TABLE_NAME = {tableName}")).Convert<int?>();
        }

        protected override void SetTableIdentCore(string tableName, int ident)
        {
            Database.ExecuteSqlInterpolated(
                $"ALTER TABLE {tableName} AUTO_INCREMENT = {ident}");
        }

        protected override Task SetTableIdentCoreAsync(string tableName, int ident)
        {
            return Database.ExecuteSqlInterpolatedAsync(
                $"ALTER TABLE {tableName} AUTO_INCREMENT = {ident}");
        }

        public override void ShrinkDatabase()
        {
            ReIndexTables();
        }

        public override Task ShrinkDatabaseAsync(CancellationToken cancelToken = default)
        {
            return ReIndexTablesAsync(cancelToken);
        }

        public override void ReIndexTables()
        {
            var tables = Database.ExecuteQueryRaw<string>($"SHOW TABLES FROM `{Database.GetDbConnection().Database}`").ToList();
            if (tables.Count > 0)
            {
                Database.ExecuteSqlRaw($"OPTIMIZE TABLE `{string.Join("`, `", tables)}`");
            }
        }

        public override async Task ReIndexTablesAsync(CancellationToken cancelToken = default)
        {
            var tables = await Database.ExecuteQueryRawAsync<string>($"SHOW TABLES FROM `{Database.GetDbConnection().Database}`", cancelToken).ToListAsync(cancelToken);
            if (tables.Count > 0)
            {
                await Database.ExecuteSqlRawAsync($"OPTIMIZE TABLE `{string.Join("`, `", tables)}`", cancelToken);
            }
        }

        public override decimal GetDatabaseSize()
        {
            return Database.ExecuteQueryRaw<MySqlTableSchema>(
                GetDatabaseSizeSql(Database.GetDbConnection().Database)).FirstOrDefault()?.SizeMB ?? 0;
        }

        public override async Task<decimal> GetDatabaseSizeAsync()
        {
            return (await Database.ExecuteQueryRawAsync<MySqlTableSchema>(
                GetDatabaseSizeSql(Database.GetDbConnection().Database)).FirstOrDefaultAsync())?.SizeMB ?? 0;
        }

        protected override IList<string> TokenizeSqlScript(string sqlScript)
        {
            var commands = new List<string>();

            var batches = Regex.Split(sqlScript, @"DELIMITER \;", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (batches.Length > 0)
            {
                commands.AddRange(
                    batches
                        .Where(b => !string.IsNullOrWhiteSpace(b))
                        .Select(b =>
                        {
                            b = Regex.Replace(b, @"(DELIMITER )?\$\$", string.Empty);
                            b = Regex.Replace(b, @"#(.*?)\r?\n", "/* $1 */");
                            b = Regex.Replace(b, @"(\r?\n)|(\t)", " ");

                            return b;
                        }));
            }

            return commands;
        }

        public override Stream OpenBlobStream(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            // TODO: (core) Check if MySql can stream blobs this way.
            return new SqlBlobStream(Database.GetDbConnection(), tableName, blobColumnName, pkColumnName, pkColumnValue);
        }
    }
}
