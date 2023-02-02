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
        ReIndex = 1 << 3,
        ComputeSize = 1 << 4,
        AccessIncrement = 1 << 5,
        StreamBlob = 1 << 6,
        ExecuteSqlScript = 1 << 7,
        StoredProcedures = 1 << 8,
        ReadSequential = 1 << 9
    }

    public abstract partial class DataProvider : Disposable
    {
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

        public bool CanShrink
        {
            get => Features.HasFlag(DataProviderFeatures.Shrink);
        }

        public bool CanReIndex
        {
            get => Features.HasFlag(DataProviderFeatures.ReIndex);
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

        #endregion

        #region Optional overridable features

        /// <summary>
        /// Gets the total size of the database in MB.
        /// </summary>
        protected virtual Task<decimal> GetDatabaseSizeCore(bool async)
            => throw new NotSupportedException();

        /// <summary>
        /// Shrinks / compacts the database
        /// </summary>
        protected virtual Task<int> ShrinkDatabaseCore(bool async, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Reindexes all tables in the current database.
        /// </summary>
        protected virtual Task<int> ReIndexTablesCore(bool async, CancellationToken cancelToken = default)
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
        /// Splits the given SQL script by provider specific delimiters.
        /// </summary>
        protected virtual IList<string> SplitSqlScript(string sqlScript)
            => throw new NotSupportedException();


        protected virtual Stream OpenBlobStreamCore(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
            => throw new NotSupportedException();

        #endregion

        #region Database schema

        public bool HasDatabase(string databaseName)
        {
            Guard.NotEmpty(databaseName);
            return HasDatabaseCore(databaseName, false).Await();
        }

        public ValueTask<bool> HasDatabaseAsync(string databaseName)
        {
            Guard.NotEmpty(databaseName);
            return HasDatabaseCore(databaseName, true);
        }

        public bool HasTable(string tableName)
        {
            Guard.NotEmpty(tableName);
            return HasTableCore(tableName, false).Await();
        }

        public ValueTask<bool> HasTableAsync(string tableName)
        {
            Guard.NotEmpty(tableName);
            return HasTableCore(tableName, true);
        }

        public bool HasColumn(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName);
            Guard.NotEmpty(columnName);
            return HasColumnCore(tableName, columnName, false).Await();
        }

        public ValueTask<bool> HasColumnAsync(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName);
            Guard.NotEmpty(columnName);
            return HasColumnCore(tableName, columnName, true);
        }

        public string[] GetTableNames()
        {
            return GetTableNamesCore(false).Await();
        }

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
        /// Gets the total size of the database in MB.
        /// </summary>
        public decimal GetDatabaseSize()
            => GetDatabaseSizeCore(false).Await();

        /// <summary>
        /// Gets the total size of the database in MB.
        /// </summary>
        public Task<decimal> GetDatabaseSizeAsync()
            => GetDatabaseSizeCore(true);

        /// <summary>
        /// Shrinks / compacts the database
        /// </summary>
        public int ShrinkDatabase()
            => ShrinkDatabaseCore(false).Await();

        /// <summary>
        /// Shrinks / compacts the database
        /// </summary>
        public Task<int> ShrinkDatabaseAsync(CancellationToken cancelToken = default)
            => ShrinkDatabaseCore(true, cancelToken);

        /// <summary>
        /// Reindexes all tables
        /// </summary>
        public int ReIndexTables()
            => ReIndexTablesCore(false).Await();

        /// <summary>
        /// Reindexes all tables
        /// </summary>
        public Task<int> ReIndexTablesAsync(CancellationToken cancelToken = default)
            => ReIndexTablesCore(true, cancelToken);

        /// <summary>
        /// Executes a (multiline) sql script
        /// </summary>
        public int ExecuteSqlScript(string sqlScript)
            => ExecuteSqlScriptCore(sqlScript, false).Await();

        /// <summary>
        /// Executes a (multiline) sql script
        /// </summary>
        public Task<int> ExecuteSqlScriptAsync(string sqlScript, CancellationToken cancelToken = default)
            => ExecuteSqlScriptCore(sqlScript, true, cancelToken);

        protected virtual async Task<int> ExecuteSqlScriptCore(string sqlScript, bool async, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(sqlScript);

            var sqlCommands = SplitSqlScript(sqlScript);
            var rowsAffected = 0;

            using var tx = async ? await Database.BeginTransactionAsync(cancelToken) : Database.BeginTransaction();
            try
            {
                foreach (var command in sqlCommands.Select(Sql))
                {
                    rowsAffected += async ? await Database.ExecuteSqlRawAsync(command, cancelToken) : Database.ExecuteSqlRaw(command);
                }

                if (async)
                {
                    await tx.CommitAsync(cancelToken);
                }
                else
                {
                    tx.Commit();
                }
                
            }
            catch
            {
                if (async)
                {
                    await tx.RollbackAsync(cancelToken);
                }
                else
                {
                    tx.Rollback();
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

        public DbParameter CreateParameter(string name, object value)
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
