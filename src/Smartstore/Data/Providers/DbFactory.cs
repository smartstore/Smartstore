using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    /// <summary>
    /// Contains factory methods for creating connection strings, <see cref="DataProvider"/>,
    /// <see cref="DbContext"/> and <see cref="DbContextOptions"/>.
    /// </summary>
    public abstract class DbFactory
    {
        private readonly static ConcurrentDictionary<string, DbFactory> _loadedFactories = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the database system type.
        /// </summary>
        public abstract DbSystemType DbSystem { get; }

        /// <summary>
        /// Creates a <see cref="DbConnectionStringBuilder"/> instance with the given <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to initialize the builder with.</param>
        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString);

        /// <summary>
        /// Creates a <see cref="DbConnectionStringBuilder"/> instance with the given parameters.
        /// </summary>
        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userName,
            string password);

        /// <summary>
        /// Tries to fix the given connection string, e.g. by checking for mandatory parameters.
        /// </summary>
        /// <param name="connectionString">The source connection string</param>
        /// <param name="normalizedConnectionString">The fixed connection string or <c>null</c> if no fix has been applied.</param>
        /// <returns><c>true</c> if any fix has been applied and the source connection string changed.</returns>
        /// <remarks>
        /// In case of SqlServer, this method will ensure that MARS is enabled and encryption
        /// is disabled on connection string level.
        /// </remarks>
        public virtual bool TryNormalizeConnectionString(string connectionString, out string normalizedConnectionString)
        {
            normalizedConnectionString = null;
            return false;
        }

        /// <summary>
        /// Creates a <see cref="DataProvider"/> instance for the current database system.
        /// </summary>
        public abstract DataProvider CreateDataProvider(DatabaseFacade database);

        /// <summary>
        /// Creates a data context configured for the current database system.
        /// </summary>
        /// <typeparam name="TContext">Data context type</typeparam>
        /// <param name="connectionString">Connection string</param>
        /// <param name="commandTimeout">Command timeout</param>
        /// <returns>The data context instance.</returns>
        public abstract TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
            where TContext : DbContext;

        /// <summary>
        /// Configures any data context to use the current database system.
        /// </summary>
        public abstract DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString);

        /// <summary>
        /// Finds the <see cref="DbFactory"/> impl type for the given <paramref name="provider"/> name
        /// and loads its assembly into the current <see cref="AssemblyLoadContext"/>.
        /// </summary>
        /// <param name="provider">The provider to find</param>
        public static DbFactory Load(string provider, ITypeScanner typeScanner)
        {
            Guard.NotEmpty(provider, nameof(provider));
            Guard.NotNull(typeScanner, nameof(typeScanner));

            return _loadedFactories.GetOrAdd(provider, key =>
            {
                var assemblyName = string.Empty;

                switch (provider.ToLowerInvariant())
                {
                    case "sqlserver":
                        assemblyName = "Smartstore.Data.SqlServer.dll";
                        break;
                    case "mysql":
                        assemblyName = "Smartstore.Data.MySql.dll";
                        break;
                        //case "sqlite":
                        //    assemblyName = "Smartstore.Data.Sqlite.dll";
                        //    break;
                }

                if (assemblyName.IsEmpty())
                {
                    throw new NotSupportedException($"Unknown database provider type name '${provider}'.");
                }

                var binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assemblyPath = Path.Combine(binPath, assemblyName);
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                var dbFactoryType = typeScanner.FindTypes<DbFactory>(new[] { assembly }).FirstOrDefault();
                if (dbFactoryType == null)
                {
                    throw new SystemException($"The data provider assembly '${assemblyName}' does not contain any concrete '${typeof(DbFactory)}' implementation.");
                }

                return (DbFactory)Activator.CreateInstance(dbFactoryType);
            });
        }
    }
}
