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

    public abstract class DataProvider : Disposable
    {
        private static readonly Regex _dbNameRegex = new(@"^(?<DbName>.+)-(?<Version>\d+(\s*\.\s*\d+){0,3})-(?<Timestamp>[0-9]{14})(?<Suffix>.+?)?",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        protected DataProvider(DatabaseFacade database)
        {
            Database = Guard.NotNull(database, nameof(database));
        }

        public DatabaseFacade Database { get; }

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

        #region Database schema

        public virtual bool HasDatabase(string databaseName)
        {
            Guard.NotEmpty(databaseName, nameof(databaseName));

            return Database.ExecuteQueryInterpolated<string>(
                $"SELECT database_id FROM sys.databases WHERE Name = {databaseName}").Any();
        }

        public virtual ValueTask<bool> HasDatabaseAsync(string databaseName)
        {
            Guard.NotEmpty(databaseName, nameof(databaseName));

            return Database.ExecuteQueryInterpolatedAsync<string>(
                $"SELECT database_id FROM sys.databases WHERE Name = {databaseName}").AnyAsync();
        }

        public virtual bool HasTable(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));

            return Database.ExecuteQueryInterpolated<string>(
                $"SELECT table_name From INFORMATION_SCHEMA.TABLES WHERE table_name = {tableName}").Any();
        }

        public virtual ValueTask<bool> HasTableAsync(string tableName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));

            return Database.ExecuteQueryInterpolatedAsync<string>(
                $"SELECT table_name From INFORMATION_SCHEMA.TABLES WHERE table_name = {tableName}").AnyAsync();
        }

        public virtual bool HasColumn(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            Guard.NotEmpty(columnName, nameof(columnName));

            return Database.ExecuteQueryInterpolated<string>(
                $"SELECT column_name From INFORMATION_SCHEMA.COLUMNS WHERE table_name = {tableName} And column_name = {columnName}").Any();
        }

        public virtual ValueTask<bool> HasColumnAsync(string tableName, string columnName)
        {
            Guard.NotEmpty(tableName, nameof(tableName));
            Guard.NotEmpty(columnName, nameof(columnName));

            return Database.ExecuteQueryInterpolatedAsync<string>(
                $"SELECT column_name From INFORMATION_SCHEMA.COLUMNS WHERE table_name = {tableName} And column_name = {columnName}").AnyAsync();
        }

        public abstract string[] GetTableNames();

        public abstract Task<string[]> GetTableNamesAsync();

        #endregion

        #region Sql / Execution strategy

        /// <summary>
        /// Encloses the given <paramref name="identifier"/> in provider specific quotes, e.g. [] for MSSQL, `` for MySql.
        /// </summary>
        /// <returns>The enclosed identifier, e.g. <c>MyColumn</c> --> <c>[MyColumn]</c>.</returns>
        public virtual string EncloseIdentifier(string identifier)
            => throw new NotSupportedException();

        /// <summary>
        /// Applies paging to <paramref name="sql"/> to limit the number of records to be returned.
        /// </summary>
        /// <returns>SQL with included paging.</returns>
        public virtual string ApplyPaging(string sql, int skip, int take)
            => throw new NotSupportedException();

        /// <summary>
        /// Executes the given INSERT INTO sql command and returns ident of the inserted row.
        /// </summary>
        /// <returns>The ident / primary key value of the newly inserted row.</returns>
        public virtual Task<int> InsertIntoAsync(string sql, params object[] parameters)
            => throw new NotSupportedException();

        /// <summary>
        /// Determines whether the specified exception represents a transient failure that can be
        /// compensated by a retry.
        /// </summary>
        /// <param name="exception">The exception object to be verified.</param>
        /// <returns>
        /// <see langword="true" /> if the specified exception is considered as transient, otherwise <see langword="false" />.
        /// </returns>
        public virtual bool IsTransientException(Exception ex)
            => false;

        /// <summary>
        /// Checks whether the inner exception indicates uniqueness violation
        /// (is 2627 = Unique constraint error, OR is 547 = Constraint check violation, OR is 2601 = Duplicated key row error)
        /// </summary>
        /// <param name="exception">The exception wrapper</param>
        /// <returns>
        /// <see langword="true" /> if the specified exception is indicates uniqueness violation, otherwise <see langword="false" />.
        /// </returns>
        public virtual bool IsUniquenessViolationException(DbUpdateException ex)
            => false;

        #endregion

        #region Maintenance

        /// <summary>
        /// Shrinks / compacts the database
        /// </summary>
        public virtual int ShrinkDatabase()
            => throw new NotSupportedException();

        /// <summary>
        /// Shrinks / compacts the database
        /// </summary>
        public virtual Task<int> ShrinkDatabaseAsync(CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Gets the total size of the database in MB.
        /// </summary>
        public virtual decimal GetDatabaseSize()
            => throw new NotSupportedException();

        /// <summary>
        /// Gets the total size of the database in MB.
        /// </summary>
        public virtual Task<decimal> GetDatabaseSizeAsync()
            => throw new NotSupportedException();

        /// <summary>
        /// Reindexes all tables
        /// </summary>
        public virtual int ReIndexTables()
            => throw new NotSupportedException();

        /// <summary>
        /// Reindexes all tables
        /// </summary>
        public virtual Task<int> ReIndexTablesAsync(CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Executes a (multiline) sql script
        /// </summary>
        public virtual void ExecuteSqlScript(string sqlScript)
        {
            var sqlCommands = TokenizeSqlScript(sqlScript);

            using var tx = Database.BeginTransaction();
            try
            {
                foreach (var command in sqlCommands)
                {
                    Database.ExecuteSqlRaw(command);
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
            }
        }

        /// <summary>
        /// Executes a (multiline) sql script
        /// </summary>
        public virtual async Task<int> ExecuteSqlScriptAsync(string sqlScript, CancellationToken cancelToken = default)
        {
            var sqlCommands = TokenizeSqlScript(sqlScript);
            var rowsAffected = 0;

            using var tx = await Database.BeginTransactionAsync(cancelToken);
            try
            {
                foreach (var command in sqlCommands)
                {
                    rowsAffected += await Database.ExecuteSqlRawAsync(command, cancelToken);
                }

                await tx.CommitAsync(cancelToken);
            }
            catch
            {
                await tx.RollbackAsync(cancelToken);
                throw;
            }

            return rowsAffected;
        }

        protected virtual IList<string> TokenizeSqlScript(string sqlScript)
            => throw new NotSupportedException();

        /// <summary>
        /// Truncates/clears a table. ALL rows will be irreversibly deleted!!!!
        /// </summary>
        public int TruncateTable<T>() where T : BaseEntity
        {
            var tableName = Context.Model.FindEntityType(typeof(T)).GetTableName();
            return Database.ExecuteSqlRaw($"TRUNCATE TABLE {tableName}");
        }

        /// <summary>
        /// Truncates/clears a table. ALL rows will be irreversibly deleted!!!!
        /// </summary>
        public Task<int> TruncateTableAsync<T>() where T : BaseEntity
        {
            var tableName = Context.Model.FindEntityType(typeof(T)).GetTableName();
            return Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {tableName}");
        }

        /// <summary>
        /// Gets the current ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Ident value or <c>null</c> if value cannot be resolved.</returns>
        public int? GetTableIdent<T>() where T : BaseEntity
        {
            return GetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName());
        }

        /// <summary>
        /// Gets the current ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <returns>Ident value or <c>null</c> if value cannot be resolved.</returns>
        public Task<int?> GetTableIdentAsync<T>() where T : BaseEntity
        {
            return GetTableIncrementCoreAsync(Context.Model.FindEntityType(typeof(T)).GetTableName());
        }

        /// <summary>
        /// Sets the table ident value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <param name="ident">The new ident value</param>
        public void SetTableIdent<T>(int ident) where T : BaseEntity
        {
            SetTableIncrementCore(Context.Model.FindEntityType(typeof(T)).GetTableName(), ident);
        }

        /// <summary>
        /// Sets the table auto increment value
        /// </summary>
        /// <typeparam name="T">Entity</typeparam>
        /// <param name="ident">The new ident value</param>
        public Task SetTableIncrementAsync<T>(int ident = 1) where T : BaseEntity
        {
            return SetTableIncrementCoreAsync(Context.Model.FindEntityType(typeof(T)).GetTableName(), ident);
        }

        protected virtual int? GetTableIncrementCore(string tableName)
            => throw new NotSupportedException();

        protected virtual Task<int?> GetTableIncrementCoreAsync(string tableName)
            => throw new NotSupportedException();

        protected virtual void SetTableIncrementCore(string tableName, int ident)
            => throw new NotSupportedException();

        protected virtual Task SetTableIncrementCoreAsync(string tableName, int ident)
            => throw new NotSupportedException();

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
            var dbName = PathUtility.SanitizeFileName(Database.GetDbConnection().Database.NaIfEmpty(), "_");

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
                var match = _dbNameRegex.Match(fileName.Trim());

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
        public virtual int BackupDatabase(string fullPath)
            => throw new NotSupportedException();

        /// <summary>
        /// Creates a database backup
        /// </summary>
        /// <param name="fullPath">The full physical path to the backup file.</param>
        public virtual Task<int> BackupDatabaseAsync(string fullPath, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupFullPath">The full physical path to the backup file to restore.</param>
        public virtual int RestoreDatabase(string backupFullPath)
            => throw new NotSupportedException();

        /// <summary>
        /// Restores a database backup
        /// </summary>
        /// <param name="backupFullPath">The full physical path to the backup file to restore.</param>
        public virtual Task<int> RestoreDatabaseAsync(string backupFullPath, CancellationToken cancelToken = default)
            => throw new NotSupportedException();

        #endregion

        #region Blob stream

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

            return OpenBlobStream(
                entityType.GetTableName(),
                entityProperty.GetColumnName(storeIdent),
                nameof(BaseEntity.Id),
                id);
        }

        public virtual Stream OpenBlobStream(string tableName, string blobColumnName, string pkColumnName, object pkColumnValue)
            => throw new NotSupportedException();

        #endregion

        #region Connection

        public abstract DbSystemType ProviderType { get; }

        public DbParameter CreateParameter(string name, object value)
        {
            Guard.NotEmpty(name, nameof(name));

            var p = CreateParameter();
            p.ParameterName = name;
            p.Value = value;

            return p;
        }

        public abstract DbParameter CreateParameter();

        #endregion
    }
}
