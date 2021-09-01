using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    /// <summary>
    /// TODO: (core) Describe everything when completed.
    /// </summary>
    public abstract class DbFactory
    {
        private readonly ConcurrentDictionary<MethodInfo, MappedDbFunction> _mappedDbFunctions = new();

        private readonly static ConcurrentDictionary<string, DbFactory> _loadedFactories = new(StringComparer.OrdinalIgnoreCase);
        
        public abstract DbSystemType DbSystem { get; }

        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString);

        public abstract DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server, 
            string database,
            string userName,
            string password);

        public abstract DataProvider CreateDataProvider(DatabaseFacade database);

        public abstract TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
            where TContext : DbContext;

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

        #region Functions

        /// <summary>
        /// Maps the given provider-agnostic <see cref="DbFunctions"/> extension method
        /// (from <see cref="DbFunctionsExtensions"/> class)
        /// to the matching provider-specific method.
        /// </summary>
        /// <param name="services">The database services</param>
        /// <param name="sourceMethod">The source method from <see cref="DbFunctionsExtensions"/> to map.</param>
        /// <param name="mappedMethod">The mapped method or <c>null</c></param>
        /// <param name="translator">The provider specific translator or <c>null</c></param>
        public bool TryMapDbFunctionsMethod(IServiceProvider services, MethodInfo sourceMethod, out MethodInfo mappedMethod, out IMethodCallTranslator translator)
        {
            var mappedFunction = _mappedDbFunctions.GetOrAdd(sourceMethod, key =>
            {
                var translator = FindMethodCallTranslator(services, sourceMethod);
                if (translator != null)
                {
                    var method = FindMappedMethod(sourceMethod);
                    if (method != null)
                    {
                        return new MappedDbFunction { Method = method, Translator = translator };
                    }
                }

                return null;
            });

            mappedMethod = mappedFunction?.Method;
            translator = mappedFunction?.Translator;

            return mappedMethod != null && translator != null;
        }

        protected abstract IMethodCallTranslator FindMethodCallTranslator(IServiceProvider services, MethodInfo sourceMethod);

        protected abstract MethodInfo FindMappedMethod(MethodInfo sourceMethod);

        class MappedDbFunction
        {
            public MethodInfo Method { get; init; }
            public IMethodCallTranslator Translator { get; init; }
        }

        #endregion
    }
}
