using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    /// <summary>
    /// TODO: (core) Describe everything when completed.
    /// </summary>
    public abstract class DbFactory
    {
        private readonly static ConcurrentDictionary<string, DbFactory> _loadedFactories = new(StringComparer.OrdinalIgnoreCase);
        
        public abstract DbSystemType DbSystem { get; }

        public abstract Type SmartDbContextType { get; }

        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString);

        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server, 
            string database,
            string userName,
            string password);

        public abstract DataProvider CreateDataProvider(DatabaseFacade database);

        public abstract HookingDbContext CreateApplicationDbContext(
            string connectionString, 
            int? commandTimeout = null, 
            string migrationHistoryTableName = null);

        public abstract DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString);

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
                    throw new SmartException($"Unknown database provider type name '${provider}'.");
                }

                var binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var assemblyPath = Path.Combine(binPath, assemblyName);
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

                var dbFactoryType = typeScanner.FindTypes<DbFactory>(new[] { assembly }).FirstOrDefault();
                if (dbFactoryType == null)
                {
                    throw new SmartException($"The data provider assembly '${assemblyName}' does not contain any concrete '${typeof(DbFactory)}' implementation.");
                }

                return (DbFactory)Activator.CreateInstance(dbFactoryType);
            });
        }

        public abstract Task<int> CreateDatabaseAsync(
            string connectionString,
            string collation = null,
            int? commandTimeout = null,
            CancellationToken cancelToken = default);
    }
}
