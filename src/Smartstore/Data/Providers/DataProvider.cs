using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Smartstore.Domain;
using Smartstore.IO;

namespace Smartstore.Data.Providers
{
    [Flags]
    public enum DataProviderFeatures
    {
        None = 0,
        Backup = 1 << 0,
        Restore = 1 << 1,
        Shrink = 1 << 2,
        OptimizeDatabase = 1 << 3,
        ComputeSize = 1 << 4,
        AccessIncrement = 1 << 5,
        StreamBlob = 1 << 6,
        ExecuteSqlScript = 1 << 7,
        StoredProcedures = 1 << 8,
        ReadSequential = 1 << 9,
        ReadTableInfo = 1 << 10,
        OptimizeTable = 1 << 11
    }

    public abstract partial class DataProvider : Disposable
    {
        const int OptimizeTimeout = 300;
        
        [GeneratedRegex("^(?<DbName>.+)-(?<Version>\\d+(\\s*\\.\\s*\\d+){0,3})-(?<Timestamp>[0-9]{14})(?<Suffix>.+?)?", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline, "de-DE")]
        private static partial Regex DbNameRegex();

        [GeneratedRegex("\\[(?<Identifier>.+?)\\]", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline)]
        private static partial Regex QuotedSqlIdentifier();

        private static readonly Regex _rgDbName = DbNameRegex();
        private static readonly Regex _rgQuotedSqlIdenfier = QuotedSqlIdentifier();

        protected DataProvider(DatabaseFacade database)
        {
            Database = Guard.NotNull(database, nameof(database));
        }

        public DatabaseFacade Database { get; }

        protected string DatabaseName 
        {
            get => Database.GetDbConnection().Database;
        }

        protected DbContext Context
        {
            get => ((IDatabaseFacadeDependenciesAccessor)Database).Context;
        }
        
        #region Feature flags

        public virtual DataProviderFeatures Features { get; } = DataProviderFeatures.None;

        public bool CanBackup
        {
            get => Features.HasFlag(DataProviderFeatures.Backup);
        }

        public bool CanRestore
        {
            get => Features.HasFlag(DataProviderFeatures.Restore);
        }

        public bool CanOptimizeDatabase
        {
            get => Features.HasFlag(DataProviderFeatures.OptimizeDatabase);
        }

        public bool CanOptimizeTable
        {
            get => Features.HasFlag(DataProviderFeatures.OptimizeTable);
        }

        public bool CanShrink
        {
            get => Features.HasFlag(DataProviderFeatures.Shrink);
        }

        public bool CanComputeSize
        {
            get => Features.HasFlag(DataProviderFeatures.ComputeSize);
        }

        public bool CanAccessIncrement
        {
            get => Features.HasFlag(DataProviderFeatures.AccessIncrement);
        }

        public bool CanStreamBlob
        {
            get => Features.HasFlag(DataProviderFeatures.StreamBlob);
        }

        public bool CanReadSequential
        {
            get => Features.HasFlag(DataProviderFeatures.ReadSequential);
        }

        public bool CanExecuteSqlScript
        {
            get => Features.HasFlag(DataProviderFeatures.ExecuteSqlScript);
        }

        public bool CanExecuteStoredProcedures
        {
            get => Features.HasFlag(DataProviderFeatures.StoredProcedures);
        }

        public bool CanReadTableInfo
        {
            get => Features.HasFlag(DataProviderFeatures.ReadTableInfo);
        }

        /// <summary>
        /// Gets a value indication whether MARS (Multiple Active Result Sets) 
        /// is enabled for the current connection.
        /// </summary>
        public abstract bool MARSEnabled { get; }

        #endregion

        #region Mandatory abstract

        /// <summary>
        /// Gets the provider type.
        /// </summary>
        public abstract DbSystemType ProviderType { get; }

        /// <summary>
        /// Creates a provider-specific <see cref="DbParameter" /> instance.
        /// </summary>
        /// <returns></returns>
        public abstract DbParameter CreateParameter();

        /// <summary>
        /// Encloses the given <paramref name="identifier"/> in provider specific quotes, e.g. [Name] for MSSQL, `Name` for MySql.
        /// </summary>
        /// <returns>The enclosed identifier, e.g. <c>MyColumn</c> --> <c>[MyColumn]</c>.</returns>
        public abstract string EncloseIdentifier(string identifier);

        /// <summary>
        /// Applies paging to <paramref name="sql"/> to limit the number of records to be returned.
        /// </summary>
        /// <returns>SQL with included paging.</returns>
        public abstract string ApplyPaging(string sql, int skip, int take);

        /// <summary>
        /// Checks whether the given database exists.
        /// </summary>
        /// <param name="databaseName">The database name to check</param>
        protected abstract ValueTask<bool> HasDatabaseCore(string databaseName, bool async);

        /// <summary>
        /// Checks whether the given table exists.
        /// </summary>
        /// <param name="tableName">The table name to check</param>
        protected abstract ValueTask<bool> HasTableCore(string tableName, bool async);

        /// <summary>
        /// Checks whether the given column exists.
        /// </summary>
        /// <param name="tableName">The table that contains the column</param>
        /// <param name="columnName">The column name to check</param>
        protected abstract ValueTask<bool> HasColumnCore(string tableName, string columnName, bool async);
        
        /// <summary>
        /// Gets all public tables in the current database
        /// </summary>
        protected abstract ValueTask<string[]> GetTableNamesCore(bool async);

        /// <summary>
        /// Truncates/clears a table.
        /// </summary>
        protected abstract Task<int> TruncateTableCore(string tableName, bool async);

        /// <summary>
        /// Executes the given INSERT INTO sql command and returns ident of the inserted row.
        /// </summary>
        /// <param name="sql">The INSERT command to execute</param>
        /// <returns>The ident / primary key value of the newly inserted row.</returns>
        protected abstract Task<int> InsertIntoCore(string sql, bool async, params object[] parameters);

        /// <summary>
        /// Determines whether the specified exception represents a transient failure that can be
        /// compensated by a retry.
        /// </summary>
        /// <param name="exception">The exception object to be verified.</param>
        /// <returns>
        /// <see langword="true" /> if the specified exception is considered as transient, otherwise <see langword="false" />.
        /// </returns>
        public abstract bool IsTransientException(Exception ex);

        /// <summary>
        /// Checks whether the inner exception indicates uniqueness violation
        /// (is 2627 = Unique constraint error, OR is 547 = Constraint check violation, OR is 2601 = Duplicated key row error)
        /// </summary>
        /// <param name="exception">The exception wrapper</param>
        /// <returns>
        /// <see langword="true" /> if the specified exception indicates uniqueness violation, otherwise <see langword="false" />.
        /// </returns>
        public abstract bool IsUniquenessViolationException(DbUpdateException ex);

        #endregion

        #region Optional overridable features

        /// <summary>
        /// Gets the database provider friendly name.
        /// </summary>
        public virtual string ProviderFriendlyName
        {
            get => ProviderType.ToString();
        }

        /// <summary>
        /// Gets the total size of the database in bytes.
        /// </summary>
        protected virtual Task<long> GetDatabaseSizeCore(bool async)
            => throw new NotSupportedException();

        /// <summary>
        /// Reorganizes the physical storage of table data and associated index data, 
        /// to reduce storage space and improve I/O efficiency when accessing tables.
        /// After successful optimization, the database is shrunk to save space.
        /// </summary>
        protected virtual Task<int> OptimizeDatabaseCore(bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Reorganizes the physical storage of table data and associated index data, 
        /// to reduce storage space and improve I/O efficiency when accessing the specified table.
        /// After successful optimization, the database is shrunk to save space.
        /// </summary
        /// <param name="tableName">Name of table to optimize.</param>
        protected virtual Task<int> OptimizeTableCore(string tableName, bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Shrinks / compacts the database.
        /// </summary>
        protected virtual Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Gets the current ident value for the given table.
        /// </summary>
        /// <param name="tableName">Table to get ident for.</param>
        /// <returns>Ident value or <c>null</c> if value cannot be resolved.</returns>
        protected virtual Task<int?> GetTableIncrementCore(string tableName, bool async)
            => throw new NotSupportedException();

        /// <summary>
        /// Sets the ident value for given table.
        /// </summary>
        /// <param name="tableName">Table to set ident for.</param>
        /// <param name="ident">The new ident value</param>
        protected virtual Task SetTableIncrementCore(string tableName, int ident, bool async)
            => throw new NotSupportedException();

        /// <summary>
        /// Creates a database backup
        /// </summary>
        /// <param name="fullPath">The full physical path to the backup file.</param>
        protected virtual Task<int> BackupDatabaseCore(string fullPath, bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupFullPath">The full physical path to the backup file to restore.</param>
        protected virtual Task<int> RestoreDatabaseCore(string backupFullPath, bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Opens a sequential BLOB stream.
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="blobColumnName">Name of BLOB column</param>
        /// <param name="pkColumnName">Name of primary key column in the given <paramref name="tableName"/>.</param>
        /// <param name="pkColumnValue">Value of primary key.</param>
        protected virtual Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
            => throw new NotSupportedException();

        /// <summary>
        /// Reads info/statistics about every public table in the database.
        /// </summary>
        protected virtual Task<List<DbTableInfo>> ReadTableInfosCore(bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        protected virtual bool SqlSupportsDelimiterStatement
        {
            get => false;
        }

        protected virtual string SqlBatchTerminator
        {
            get => null;
        }

        /// <summary>
        /// Splits the given SQL script by provider specific delimiters.
        /// </summary>
        protected virtual IList<string> SplitSqlScript(string sqlScript)
        {
            var delimiter = ";";
            var canTerminateBatch = SqlBatchTerminator.HasValue();
            var inMultilineComment = false;
            var commands = new List<string>();
            var command = string.Empty;
            var lines = sqlScript.ReadLines(true);

            foreach (var line in lines)
            {
                // Ignore comments
                var commandLine = ReadSqlCommandLine(line, ref inMultilineComment);
                if (commandLine.IsEmpty())
                {
                    continue;
                }

                // In some DB systems (e.g. MySQL), you can change the delimiter using the DELIMITER statement.
                // To handle this scenario, we need to track the current delimiter
                // and change it whenever we encounter a DELIMITER statement
                if (SqlSupportsDelimiterStatement && commandLine.StartsWithNoCase("DELIMITER"))
                {
                    delimiter = commandLine.Split(' ')[1].Trim();
                    continue;
                }

                // MSSQL can terminate batches with the "GO" statement
                var isBatchTerminator = canTerminateBatch && commandLine.EqualsNoCase(SqlBatchTerminator);

                if (isBatchTerminator)
                {
                    if (command.HasValue())
                    {
                        commands.Add(command);
                        command = string.Empty;
                    }
                }
                else if (!commandLine.EndsWith(delimiter))
                {
                    command += commandLine + Environment.NewLine;
                }
                else
                {
                    command += commandLine[..^delimiter.Length];
                    commands.Add(command.Trim());
                    command = string.Empty;
                }
            }

            if (command.Length > 0)
            {
                commands.Add(command.Trim());
            }

            return commands;
        }

        /// <summary>
        /// Reads a single sql command line while skipping single- and multi-line comments.
        /// </summary>
        protected virtual string ReadSqlCommandLine(string line, ref bool inMultilineComment)
        {
            if (line.IsEmpty())
            {
                return line;
            }

            if (inMultilineComment)
            {
                var endCommentIndex = line.IndexOf("*/");
                if (endCommentIndex > -1)
                {
                    inMultilineComment = false;
                    line = line[(endCommentIndex + 2)..];
                }
                else
                {
                    line = string.Empty;
                }
            }

            var singleLineCommentIndex = line.IndexOf("--");
            if (singleLineCommentIndex > -1)
            {
                line = line[..singleLineCommentIndex];
            }
            else
            {
                singleLineCommentIndex = line.IndexOf('#');
                if (singleLineCommentIndex > -1)
                {
                    line = line[..singleLineCommentIndex];
                }
            }

            var startCommentIndex = line.IndexOf("/*");
            if (startCommentIndex > -1)
            {
                var endCommentIndex = line.IndexOf("*/", startCommentIndex + 2);
                if (endCommentIndex > -1)
                {
                    line = string.Concat(line.AsSpan(0, startCommentIndex), line.AsSpan(endCommentIndex + 2));
                }
                else
                {
                    inMultilineComment = true;
                    line = line[..startCommentIndex];
                }
            }

            return line.Trim();
        }

        #endregion

        #region Database schema

        /// <summary>
        /// Checks whether the database server instance contains the given database.
        /// </summary>
        public bool HasDatabase(string databaseName)
        {
            Guard.NotEmpty(databaseName);
            return HasDatabaseCore(databaseName, false).Await();
        }

        /// <summary>
        /// Checks whether the database server instance contains the given database.
        /// </summary>
        public ValueTask<bool> HasDatabaseAsync(string databaseName)
        {
            Guard.NotEmpty(databaseName);
            return HasDatabaseCore(databaseName, true);
        }

        /// <summary>
        /// Checks whether the database contains the given table.
        /// </summary>
        public bool HasTable(string tableName)
        {
            Guard.NotEmpty(tableName);
            return HasTableCore(tableName, false).Await();
        }

        /// <summary>
        /// Checks whether the database contains the given table.
        /// </summary>
        public ValueTask<bool> HasTableAsync(string tableName)
        {
            Guard.NotEmpty(tableName);
            return HasTableCore(tableName, true);
        }

        /// <summary>
        /// Checks whether the a table contains the given column.
        /// </summary>
        public bool HasColumn(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName);
            Guard.NotEmpty(columnName);
            return HasColumnCore(tableName, columnName, false).Await();
        }

        /// <summary>
        /// Checks whether the a table contains the given column.
        /// </summary>
        public ValueTask<bool> HasColumnAsync(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName);
            Guard.NotEmpty(columnName);
            return HasColumnCore(tableName, columnName, true);
        }

        /// <summary>
        /// Gets all names of public table contained in the current database.
        /// </summary>
        public string[] GetTableNames()
        {
            return GetTableNamesCore(false).Await();
        }

        /// <summary>
        /// Gets all names of public table contained in the current database.
        /// </summary>
        public ValueTask<string[]> GetTableNamesAsync()
        {
            return GetTableNamesCore(true);
        }

        #endregion

        #region Data

        /// <summary>
        /// Executes the given INSERT INTO sql command and returns ident of the inserted row.
        /// </summary>
        /// <param name="sql">The INSERT command to execute</param>
        /// <returns>The ident / primary key value of the newly inserted row.</returns>
        public int InsertInto(string sql, params object[] parameters)
        {
            Guard.NotEmpty(sql);
            return InsertIntoCore(Sql(sql), false, parameters).Await();
        }

        /// <summary>
        /// Executes the given INSERT INTO sql command and returns ident of the inserted row.
        /// </summary>
        /// <param name="sql">The INSERT command to execute</param>
        /// <returns>The ident / primary key value of the newly inserted row.</returns>
        public Task<int> InsertIntoAsync(string sql, params object[] parameters)
        {
            Guard.NotEmpty(sql);
            return InsertIntoCore(Sql(sql), true, parameters);
        }

        public Stream OpenBlobStream<T, TProp>(Expression<Func<T, TProp>> propertyAccessor, int id)
            where T : BaseEntity
        {
            Guard.NotNull(propertyAccessor, nameof(propertyAccessor));
            Guard.IsPositive(id, nameof(id));

            var model = Context.Model;

            var entityType = model.FindEntityType(typeof(T));
            if (entityType == null)
            {
                throw new ArgumentException($"The entity type '{typeof(T)}' is not associated with the current database context.", "T");
            }

            var propName = propertyAccessor.ExtractMemberInfo().Name;
            var entityProperty = entityType.GetProperty(propName);
            if (entityProperty == null)
            {
                throw new ArgumentException($"The property '{propName}' is not mapped to the database.", nameof(propertyAccessor));
            }

            var storeIdent = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table).Value;

            return OpenBlobStreamCore(
                entityType.GetTableName(),
                entityProperty.GetColumnName(storeIdent),
                nameof(BaseEntity.Id),
                id);
        }

        #endregion

        #region Maintenance
        
        /// <summary>
        /// Gets the total size of the database in bytes.
        /// </summary>
        public long GetDatabaseSize()
            => GetDatabaseSizeCore(false).Await();

        /// <summary>
        /// Gets the total size of the database in bytes.
        /// </summary>
        public Task<long> GetDatabaseSizeAsync()
            => GetDatabaseSizeCore(true);

        /// <summary>
        /// Optimizes all table data and associated index data to reduce storage space and improve I/O efficiency.
        /// </summary>
        public int OptimizeDatabase()
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return OptimizeDatabaseCore(false).Await();
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Optimizes all table data and associated index data to reduce storage space and improve I/O efficiency.
        /// </summary>
        public Task<int> OptimizeDatabaseAsync(CancellationToken cancelToken = default)
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return OptimizeDatabaseCore(true, cancelToken);
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Reorganizes the physical storage of table data and associated index data, 
        /// to reduce storage space and improve I/O efficiency when accessing the specified table.
        /// </summary
        /// <param name="tableName">Name of table to optimize.</param>
        public int OptimizeTable(string tableName)
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return OptimizeTableCore(tableName, false).Await();
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Reorganizes the physical storage of table data and associated index data, to reduce storage space and improve I/O efficiency when accessing the specified table.
        /// </summary
        /// <param name="tableName">Name of table to optimize.</param>
        public Task<int> OptimizeTableAsync(string tableName, CancellationToken cancelToken = default)
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return OptimizeTableCore(tableName, true, cancelToken);
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Shrinks / compacts the database.
        /// </summary>
        public int ShrinkDatabase()
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return ShrinkDatabaseCore(false).Await();
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Shrinks / compacts the database.
        /// </summary>
        public Task<int> ShrinkDatabaseAsync(CancellationToken cancelToken = default)
        {
            var timeout = Database.GetCommandTimeout();
            try
            {
                Database.SetCommandTimeout(OptimizeTimeout); // 5 min.
                return ShrinkDatabaseCore(true, cancelToken);
            }
            finally
            {
                Database.SetCommandTimeout(timeout);
            }
        }

        /// <summary>
        /// Reads info/statistics about every public table in the database.
        /// </summary>
        public List<DbTableInfo> ReadTableInfos()
            => ReadTableInfosCore(false).Await();

        /// <summary>
        /// Reads info/statistics about every public table in the database.
        /// </summary>
        public Task<List<DbTableInfo>> ReadTableInfosAsync(CancellationToken cancelToken = default)
            => ReadTableInfosCore(true, cancelToken);

        /// <summary>
        /// Executes a (multiline) sql script in an atomic transaction.
        /// </summary>
        public int ExecuteSqlScript(string sqlScript)
            => ExecuteSqlScriptCore(sqlScript, false).Await();

        /// <summary>
        /// Executes a (multiline) sql script in an atomic transaction.
        /// </summary>
        public Task<int> ExecuteSqlScriptAsync(string sqlScript, CancellationToken cancelToken = default)
            => ExecuteSqlScriptCore(sqlScript, true, cancelToken);

        protected virtual async Task<int> ExecuteSqlScriptCore(string sqlScript, bool async, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(sqlScript);

            var sqlCommands = SplitSqlScript(sqlScript);

            if (sqlCommands.Count == 0)
            {
                return 0;
            }

            var rowsAffected = 0;
            var isInTransaction = Database.CurrentTransaction != null;

            using var tx = isInTransaction
                ? null
                : (async ? await Database.BeginTransactionAsync(cancelToken) : Database.BeginTransaction());

            try
            {
                foreach (var command in sqlCommands.Select(Sql))
                {
                    rowsAffected += async ? await Database.ExecuteSqlRawAsync(command, cancelToken) : Database.ExecuteSqlRaw(command);
                }

                if (!isInTransaction)
                {
                    if (async)
                    {
                        await tx.CommitAsync(cancelToken);
                    }
                    else
                    {
                        tx.Commit();
                    }
                }            
            }
            catch
            {
                if (!isInTransaction)
                {
                    if (async)
                    {
                        await tx.RollbackAsync(cancelToken);
                    }
                    else
                    {
                        tx.Rollback();
                    }
                }

                throw;
            }

            return rowsAffected;
        }

        /// <summary>
        /// Truncates/clears a table. ALL rows will be irreversibly deleted!!!!
        /// </summary>
        public int TruncateTable<T>() where T : BaseEntity
        {
            return TruncateTableCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), false).Await();
        }

        /// <summary>
        /// Truncates/clears a table. ALL rows will be irreversibly deleted!!!!
        /// </summary>
        public Task<int> TruncateTableAsync<T>() where T : BaseEntity
        {
            return TruncateTableCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), true);
        }

        /// <summary>
        /// Gets the current ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Ident value or <c>null</c> if value cannot be resolved.</returns>
        public int? GetTableIdent<T>() where T : BaseEntity
        {
            return GetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), false).Await();
        }

        /// <summary>
        /// Gets the current ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Ident value or <c>null</c> if value cannot be resolved.</returns>
        public Task<int?> GetTableIdentAsync<T>() where T : BaseEntity
        {
            return GetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), true);
        }

        /// <summary>
        /// Sets the table ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <param name="ident">The new ident value</param>
        public void SetTableIdent<T>(int ident) where T : BaseEntity
        {
            SetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), ident, false).Await();
        }

        /// <summary>
        /// Sets the table auto increment value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <param name="ident">The new ident value</param>
        public Task SetTableIncrementAsync<T>(int ident = 1) where T : BaseEntity
        {
            return SetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), ident, true);
        }

        #endregion

        #region Backup

        /// <summary>
        /// Gets or sets the file extension (including the period ".") of a database backup. ".bak" by default.
        /// </summary>
        protected virtual string BackupFileExtension => ".bak";

        /// <summary>
        /// Creates a file name for a database backup with the format:
        /// {database name}-{Smartstore version}-{timestamp}{<see cref="BackupFileExtension"/>}
        /// </summary>
        public virtual string CreateBackupFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var dbName = PathUtility.SanitizeFileName(DatabaseName.NaIfEmpty(), "_");

            return $"{dbName}-{SmartstoreVersion.CurrentFullVersion}-{timestamp}{BackupFileExtension}";
        }

        /// <summary>
        /// Validates the file name of a database backup.
        /// </summary>
        /// <param name="fileName">File name of a database backup.</param>
        public virtual DbBackupValidationResult ValidateBackupFileName(string fileName)
        {
            if (fileName.HasValue())
            {
                var match = _rgDbName.Match(fileName.Trim());

                if (match.Success
                    && Version.TryParse(match.Groups["Version"].Value, out var version)
                    && DateTime.TryParseExact(match.Groups["Timestamp"].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
                    && Path.GetExtension(fileName).EqualsNoCase(BackupFileExtension))
                {
                    return new DbBackupValidationResult(fileName)
                    {
                        IsValid = true,
                        Version = version,
                        Timestamp = timestamp
                    };
                }
            }

            return new DbBackupValidationResult(fileName);
        }

        /// <summary>
        /// Creates a database backup
        /// </summary>
        /// <param name="fullPath">The full physical path to the backup file.</param>
        public int BackupDatabase(string fullPath)
        {
            Guard.NotEmpty(fullPath);
            return BackupDatabaseCore(fullPath, false).Await();
        }

        /// <summary>
        /// Creates a database backup
        /// </summary>
        /// <param name="fullPath">The full physical path to the backup file.</param>
        public Task<int> BackupDatabaseAsync(string fullPath, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(fullPath);
            return BackupDatabaseCore(fullPath, true, cancelToken);
        }

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupFullPath">The full physical path to the backup file to restore.</param>
        public int RestoreDatabase(string backupFullPath)
        {
            Guard.NotEmpty(backupFullPath);
            return RestoreDatabaseCore(backupFullPath, false).Await();
        }

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupFullPath">The full physical path to the backup file to restore.</param>
        public Task<int> RestoreDatabaseAsync(string backupFullPath, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(backupFullPath);
            return RestoreDatabaseCore(backupFullPath, true, cancelToken);
        }

        #endregion

        #region Connection & Dialect

        public virtual DbParameter CreateParameter(string name, object value)
        {
            Guard.NotEmpty(name);

            var p = CreateParameter();
            p.ParameterName = name;
            p.Value = value;

            return p;
        }

        /// <summary>
        /// Normalizes given <paramref name="sql"/> command text by replacing
        /// quoted identifiers in MSSQL dialect to provider-specific quotes. E.g.:
        /// SELECT [Id] FROM [Customers] --> SELECT `Id` FROM `Customers` (MySql dialect).
        /// </summary>
        /// <param name="sql">The sql command text to normalize</param>
        /// <returns>The normalized sql command text.</returns>
        /// <remarks>
        /// To keep the method name short, it's called "Sql" instead of "NormalizeSql".
        /// </remarks>
        public string Sql(string sql)
        {
            Guard.NotEmpty(sql);
            
            if (ProviderType == DbSystemType.SqlServer)
            {
                return sql;
            }

            var normalizedSql = _rgQuotedSqlIdenfier.Replace(sql, match =>
            {
                var identifier = match.Groups["Identifier"].Value;
                return EncloseIdentifier(identifier);
            });

            return normalizedSql;
        }

        #endregion
    }
}
