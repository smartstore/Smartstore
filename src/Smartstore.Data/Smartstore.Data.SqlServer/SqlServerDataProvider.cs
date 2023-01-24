using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        private static string ReIndexTablesSql(string database)
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

        public override DbSystemType ProviderType => DbSystemType.SqlServer;

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
            var sql = $"TRUNCATE TABLE {EncloseIdentifier(tableName)}";
            return async
                ? Database.ExecuteSqlRawAsync(sql)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        public override async Task<int> InsertIntoAsync(string sql, params object[] parameters)
        {
            Guard.NotEmpty(sql, nameof(sql));
            return (await Database.ExecuteQueryRawAsync<decimal>(
                sql + "; SELECT @@IDENTITY;", parameters).FirstOrDefaultAsync()).Convert<int>();
        }

        public override bool IsTransientException(Exception ex)
        {
            return DetectSqlError(ex, _transientErrorCodes);
        }

        public override bool IsUniquenessViolationException(DbUpdateException updateException)
        {
            return DetectSqlError(updateException?.InnerException, _uniquenessViolationErrorCodes);
        }

        protected override Task<decimal> GetDatabaseSizeCore(bool async)
        {
            var sql = "SELECT SUM(size) / 128.0 FROM sysfiles";
            return async
                ? Database.ExecuteScalarRawAsync<decimal>(sql)
                : Task.FromResult(Database.ExecuteScalarRaw<decimal>(sql));
        }

        protected override Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = "DBCC SHRINKDATABASE(0)";
            return async
                ? Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
        }

        protected override Task<int> ReIndexTablesCore(bool async, CancellationToken cancelToken = default)
        {
            var sql = ReIndexTablesSql(DatabaseName);
            return async
                ? Database.ExecuteSqlRawAsync(sql, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(sql));
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

        protected override Task<int> RestoreDatabaseCore(string backupFullPath, bool async, CancellationToken cancelToken = default)
        {
            return async
                ? Database.ExecuteSqlRawAsync(RestoreDatabaseSql(DatabaseName), new object[] { backupFullPath }, cancelToken)
                : Task.FromResult(Database.ExecuteSqlRaw(RestoreDatabaseSql(DatabaseName), new object[] { backupFullPath }));
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

        protected override Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
        {
            return new SqlBlobStream(Database, tableName, blobColumnName, pkColumnName, pkColumnValue);
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
    }
}
