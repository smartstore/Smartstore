using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data.DataProviders
{
    public class SqlServerDataProvider : DataProvider
    {
        public SqlServerDataProvider(DatabaseFacade database)
            : base(database)
        {
        }

        private string ReIndexTablesSql(string database)
            => $@"DECLARE @TableName sysname 
                  DECLARE cur_reindex CURSOR FOR
                  SELECT table_name
                  FROM [{database}].information_schema.tables
                  WHERE table_type = 'base table'
                  OPEN cur_reindex
                  FETCH NEXT FROM cur_reindex INTO @TableName
                  WHILE @@FETCH_STATUS = 0
                      BEGIN
                          EXEC('ALTER INDEX ALL ON [' + @TableName + '] REBUILD')
                          FETCH NEXT FROM cur_reindex INTO @TableName
                      END
                  CLOSE cur_reindex
                  DEALLOCATE cur_reindex";

        private string RestoreDatabaseSql(string database)
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

        public override DataProviderFeatures Features 
            => DataProviderFeatures.Backup | DataProviderFeatures.ComputeSize | DataProviderFeatures.ReIndex | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.Restore | DataProviderFeatures.AccessIdent | DataProviderFeatures.Shrink | DataProviderFeatures.StreamBlob;

        public override void ShrinkDatabase()
        {
            Database.ExecuteSqlRaw("DBCC SHRINKDATABASE(0)");
        }

        public override Task ShrinkDatabaseAsync(CancellationToken cancelToken = default)
        {
            return Database.ExecuteSqlRawAsync("DBCC SHRINKDATABASE(0)", cancelToken);
        }

        public override decimal GetDatabaseSize()
        {
            return Database.ExecuteScalarRaw<decimal>("SELECT SUM(size) / 128.0 FROM sysfiles");
        }

        public override Task<decimal> GetDatabaseSizeAsync()
        {
            return Database.ExecuteScalarRawAsync<decimal>("SELECT SUM(size) / 128.0 FROM sysfiles");
        }

        protected override int? GetTableIdentCore(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return Database.ExecuteScalarRaw<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')").Convert<int?>();
        }

        protected override async Task<int?> GetTableIdentCoreAsync(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return (await Database.ExecuteScalarRawAsync<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')")).Convert<int?>();
        }

        protected override void SetTableIdentCore(string tableName, int ident)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            Database.ExecuteSqlRaw(
                $"DBCC CHECKIDENT([{tableName}], RESEED, {ident})");
        }

        protected override Task SetTableIdentCoreAsync(string tableName, int ident)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return Database.ExecuteSqlRawAsync(
                $"DBCC CHECKIDENT([{tableName}], RESEED, {ident})");
        }

        public override void ReIndexTables()
        {
            Database.ExecuteSqlRaw(ReIndexTablesSql(Database.GetDbConnection().Database));
        }

        public override Task ReIndexTablesAsync(CancellationToken cancelToken = default)
        {
            return Database.ExecuteSqlRawAsync(ReIndexTablesSql(Database.GetDbConnection().Database), cancelToken);
        }

        public override void BackupDatabase(string fullPath)
        {
            Guard.NotEmpty(fullPath, nameof(fullPath));

            Database.ExecuteSqlRaw(
                "BACKUP DATABASE [" + Database.GetDbConnection().Database + "] TO DISK = {0} WITH FORMAT", fullPath);
        }

        public override Task BackupDatabaseAsync(string fullPath, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(fullPath, nameof(fullPath));

            return Database.ExecuteSqlRawAsync(
                "BACKUP DATABASE [" + Database.GetDbConnection().Database + "] TO DISK = {0} WITH FORMAT", new object[] { fullPath }, cancelToken);
        }

        public override void RestoreDatabase(string backupFullPath)
        {
            Guard.NotEmpty(backupFullPath, nameof(backupFullPath));

            Database.ExecuteSqlRaw(
                RestoreDatabaseSql(Database.GetDbConnection().Database), 
                backupFullPath);
        }

        public override Task RestoreDatabaseAsync(string backupFullPath, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(backupFullPath, nameof(backupFullPath));

            return Database.ExecuteSqlRawAsync(
                RestoreDatabaseSql(Database.GetDbConnection().Database),
                new object[] { backupFullPath },
                cancelToken);
        }

        protected override IList<string> TokenizeSqlScript(string sqlScript)
        {
            Guard.NotEmpty(sqlScript, nameof(sqlScript));

            var commands = new List<string>();

            sqlScript = Regex.Replace(sqlScript, @"\\\r?\n", string.Empty);
            var batches = Regex.Split(sqlScript, @"^\s*(GO[ \t]+[0-9]+|GO)(?:\s+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            for (var i = 0; i < batches.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(batches[i]) || batches[i].StartsWith("GO", StringComparison.OrdinalIgnoreCase))
                    continue;

                var count = 1;
                if (i != batches.Length - 1 && batches[i + 1].StartsWith("GO", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(batches[i + 1], "([0-9]+)");
                    if (match.Success)
                        count = int.Parse(match.Value);
                }

                var builder = new StringBuilder();
                for (var j = 0; j < count; j++)
                {
                    builder.Append(batches[i]);
                    if (i == batches.Length - 1)
                        builder.AppendLine();
                }

                commands.Add(builder.ToString());
            }

            return commands;
        }

        public override Stream OpenBlobStream(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            return new SqlBlobStream(Database.GetDbConnection(), tableName, blobColumnName, pkColumnName, pkColumnValue);
        }
    }
}
