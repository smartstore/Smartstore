using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    public class DbFunctionMap
    {
        public MethodInfo Method { get; init; }
        public IMethodCallTranslator Translator { get; init; }
    }

    /// <summary>
    /// Contains factory methods for creating connection strings, <see cref="DataProvider"/>,
    /// <see cref="DbContext"/> and <see cref="DbContextOptions"/>.
    /// </summary>
    public abstract class DbFactory
    {
        private readonly ConcurrentDictionary<MethodInfo, DbFunctionMap> _mappedDbFunctions = new();

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

        #region Functions

        /// <summary>
        /// Maps the given provider-agnostic <see cref="DbFunctions"/> extension method
        /// (from <see cref="DbFunctionsExtensions"/> class)
        /// to the matching provider-specific method.
        /// </summary>
        /// <param name="services">The database services</param>
        /// <param name="sourceMethod">The source method from <see cref="DbFunctionsExtensions"/> to map.</param>
        /// <returns>Information about the target provider-specific method and translator.</returns>
        public DbFunctionMap MapDbFunction(IServiceProvider services, MethodInfo sourceMethod)
        {
            var mappedFunction = _mappedDbFunctions.GetOrAdd(sourceMethod, key =>
            {
                var translator = FindMethodCallTranslator(services, sourceMethod);
                if (translator != null)
                {
                    var method = FindMappedMethod(sourceMethod);
                    if (method != null)
                    {
                        return new DbFunctionMap { Method = method, Translator = translator };
                    }
                }

                return null;
            });

            return mappedFunction;
        }

        protected abstract IMethodCallTranslator FindMethodCallTranslator(IServiceProvider services, MethodInfo sourceMethod);

        protected abstract MethodInfo FindMappedMethod(MethodInfo sourceMethod);

        #endregion
    }
}
