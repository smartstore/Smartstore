using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Providers;

namespace Smartstore.Data.SqlServer
{
    public class SqlServerDataProvider : DataProvider
    {
        private readonly static HashSet<int> _transientErrorCodes = new(new[]
        {
            49920, // Cannot process request. Too many operations in progress for subscription "%ld".
            49919, // Cannot process create or update request. Too many create or update operations in progress for subscription "%ld".
            41305, // The current transaction failed to commit due to a repeatable read validation failure.
            41302, // The current transaction attempted to update a record that has been updated since the transaction started.
            41301, // Dependency failure: a dependency was taken on another transaction that later failed to commit.
            10936, // Resource ID : %d. The request limit for the elastic pool is %d and has been reached.
            1205,  // Deadlock
            20     // This exception can be thrown even if the operation completed successfully, so it's safer to let the application fail.
        });

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

        public override DbSystemType ProviderType => DbSystemType.SqlServer;

        public override DataProviderFeatures Features
            => DataProviderFeatures.Backup | DataProviderFeatures.ComputeSize | DataProviderFeatures.ReIndex | DataProviderFeatures.ExecuteSqlScript
            | DataProviderFeatures.Restore | DataProviderFeatures.AccessIncrement | DataProviderFeatures.Shrink | DataProviderFeatures.StreamBlob
            | DataProviderFeatures.StoredProcedures;

        public override int ShrinkDatabase()
        {
            return Database.ExecuteSqlRaw("DBCC SHRINKDATABASE(0)");
        }

        public override Task<int> ShrinkDatabaseAsync(CancellationToken cancelToken = default)
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

        public override async Task<int> InsertIntoAsync(string sql, params object[] parameters)
        {
            // TODO: (core) Test InsertIntoAsync with SqlServer & MySql
            Guard.NotEmpty(sql, nameof(sql));
            return (await Database.ExecuteQueryRawAsync<decimal>(
                sql + "; SELECT @@IDENTITY;", parameters).FirstOrDefaultAsync()).Convert<int>();
        }

        protected override int? GetTableIncrementCore(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return Database.ExecuteScalarRaw<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')").Convert<int?>();
        }

        protected override async Task<int?> GetTableIncrementCoreAsync(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return (await Database.ExecuteScalarRawAsync<decimal?>(
                $"SELECT IDENT_CURRENT('[{tableName}]')")).Convert<int?>();
        }

        protected override void SetTableIncrementCore(string tableName, int ident)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            Database.ExecuteSqlRaw(
                $"DBCC CHECKIDENT([{tableName}], RESEED, {ident})");
        }

        protected override Task SetTableIncrementCoreAsync(string tableName, int ident)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            return Database.ExecuteSqlRawAsync(
                $"DBCC CHECKIDENT([{tableName}], RESEED, {ident})");
        }

        public override int ReIndexTables()
        {
            return Database.ExecuteSqlRaw(ReIndexTablesSql(Database.GetDbConnection().Database));
        }

        public override Task<int> ReIndexTablesAsync(CancellationToken cancelToken = default)
        {
            return Database.ExecuteSqlRawAsync(ReIndexTablesSql(Database.GetDbConnection().Database), cancelToken);
        }

        public override int BackupDatabase(string fullPath)
        {
            Guard.NotEmpty(fullPath, nameof(fullPath));

            return Database.ExecuteSqlRaw(
                "BACKUP DATABASE [" + Database.GetDbConnection().Database + "] TO DISK = {0} WITH FORMAT", fullPath);
        }

        public override Task<int> BackupDatabaseAsync(string fullPath, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(fullPath, nameof(fullPath));

            return Database.ExecuteSqlRawAsync(
                "BACKUP DATABASE [" + Database.GetDbConnection().Database + "] TO DISK = {0} WITH FORMAT", new object[] { fullPath }, cancelToken);
        }

        public override int RestoreDatabase(string backupFullPath)
        {
            Guard.NotEmpty(backupFullPath, nameof(backupFullPath));

            return Database.ExecuteSqlRaw(
                RestoreDatabaseSql(Database.GetDbConnection().Database),
                backupFullPath);
        }

        public override Task<int> RestoreDatabaseAsync(string backupFullPath, CancellationToken cancelToken = default)
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
            return new SqlBlobStream(Database, tableName, blobColumnName, pkColumnName, pkColumnValue);
        }

        public override bool IsTransientException(Exception ex)
        {
            return DetectSqlError(ex, _transientErrorCodes);
        }

        public override bool IsUniquenessViolationException(DbUpdateException updateException)
        {
            return DetectSqlError(updateException?.InnerException, _uniquenessViolationErrorCodes);
        }

        public override DbParameter CreateParameter()
        {
            return new SqlParameter();
        }

        private static bool DetectSqlError(Exception ex, ICollection<int> errorCodes)
        {
            // TODO: (core) Implement Sql error detection for other data providers.
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
    }
}
