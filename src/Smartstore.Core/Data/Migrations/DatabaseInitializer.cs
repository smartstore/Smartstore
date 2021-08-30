using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentMigrator;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Engine;

// TODO: (mg) (core) We need to separate migration infrastructure and actual migration classes. Both cannot exist in the same space.
//       Maybe we can move infrastructure to "Smartstore" assembly.

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Responsible for initializing all migratable <see cref="DbContext"/> instances on app startup.
    /// </summary>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initializes / migrates all discovered migratable <see cref="DbContext"/> instances.
        /// </summary>
        Task InitializeDatabasesAsync(CancellationToken cancelToken);

        /// <summary>
        /// Initializes / migrates the given <paramref name="dbContextType"/>.
        /// </summary>
        Task InitializeDatabaseAsync(Type dbContextType, CancellationToken cancelToken);
    }

    public static class IDatabaseInitializerExtensions
    {
        /// <summary>
        /// Initializes / migrates the given <typeparamref name="TContext"/> type.
        /// </summary>
        public static Task InitializeDatabaseAsync<TContext>(this IDatabaseInitializer initializer, CancellationToken cancelToken = default)
            => initializer.InitializeDatabaseAsync(typeof(TContext), cancelToken);
    }


    public class DatabaseInitializer : IDatabaseInitializer
    {
        private static readonly SyncedCollection<Type> _initializedContextTypes = new List<Type>().AsSynchronized();
        private readonly ILifetimeScope _scope;
        private readonly SmartConfiguration _appConfig;
        private readonly ITypeScanner _typeScanner;
        private readonly Multimap<Type, Type> _seedersMap;

        public DatabaseInitializer(
            ILifetimeScope scope, 
            ITypeScanner typeScanner, 
            SmartConfiguration appConfig)
        {
            _scope = scope;
            _appConfig = appConfig;
            _typeScanner = typeScanner;
            _seedersMap = DiscoverDataSeeders().ToMultimap(key => key.ContextType, value => value.SeederType);
        }

        public virtual async Task InitializeDatabasesAsync(CancellationToken cancelToken = default)
        {
            var contextTypes = _typeScanner.FindTypes<DbContext>().ToArray();

            foreach (var contextType in contextTypes)
            {
                await InitializeDatabaseAsync(contextType, cancelToken);
            }
        }

        public Task InitializeDatabaseAsync(Type dbContextType, CancellationToken cancelToken = default)
        {
            Guard.NotNull(dbContextType, nameof(dbContextType));
            Guard.IsAssignableFrom<DbContext>(dbContextType);

            var migrator = _scope.Resolve(typeof(DbMigrator<>).MakeGenericType(dbContextType)) as DbMigrator;
            return InitializeDatabaseAsync(migrator, _seedersMap[dbContextType], cancelToken);
        }

        protected virtual async Task InitializeDatabaseAsync(DbMigrator migrator, IEnumerable<Type> seederTypes, CancellationToken cancelToken = default)
        {
            Guard.NotNull(migrator, nameof(migrator));

            var context = migrator.Context;
            var type = context.GetInvariantType();

            if (_initializedContextTypes.Contains(type))
            {
                return;
            }

            if (!await context.Database.CanConnectAsync(cancelToken))
            {
                throw new InvalidOperationException($"Database migration failed because the target database does not exist. Ensure the database was initialized and properly seeded with data.");
            }

            using (new DbContextScope(context, minHookImportance: HookImportance.Essential))
            {
                // Set (usually longer) command timeout for migrations
                var prevCommandTimeout = context.Database.GetCommandTimeout();
                if (_appConfig.DbMigrationCommandTimeout.HasValue && _appConfig.DbMigrationCommandTimeout.Value > 15)
                {
                    context.Database.SetCommandTimeout(_appConfig.DbMigrationCommandTimeout.Value);
                }
                
                // Run all pending migrations
                await migrator.RunPendingMigrationsAsync(cancelToken);

                // Execute the global seeders anyway (on every startup),
                // we could have locale resources or settings to add/update.
                await RunGlobalSeeders(context, seederTypes, cancelToken);

                // Restore standard command timeout
                context.Database.SetCommandTimeout(prevCommandTimeout);

                _initializedContextTypes.Add(type);
            }
        }

        private static async Task RunGlobalSeeders(HookingDbContext dbContext, IEnumerable<Type> seederTypes, CancellationToken cancelToken = default)
        {
            foreach (var seederType in seederTypes)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                
                var seeder = Activator.CreateInstance(seederType);
                if (seeder != null)
                {
                    var seedMethod = seederType.GetMethod(nameof(IDataSeeder<HookingDbContext>.SeedAsync), BindingFlags.Public | BindingFlags.Instance);
                    if (seedMethod != null)
                    {
                        await (Task)seedMethod.Invoke(seeder, new object[] { dbContext, cancelToken });
                        await dbContext.SaveChangesAsync(cancelToken);
                    }
                }
            }
        }

        protected virtual IEnumerable<(Type SeederType, Type ContextType)> DiscoverDataSeeders()
        {
            var seederTypes = _typeScanner.FindTypes(typeof(IDataSeeder<>), ignoreInactiveModules: true);

            foreach (var seederType in seederTypes)
            {
                if (!seederType.HasDefaultConstructor())
                {
                    // Skip data seeders that are not constructible.
                    continue;
                }

                if (typeof(IMigration).IsAssignableFrom(seederType))
                {
                    // Skip data seeders that are bound to specific migrations.
                    continue;
                }

                if (seederType.IsSubClass(typeof(IDataSeeder<>), out var intf))
                {
                    yield return (seederType, intf.GetGenericArguments()[0]);
                }
            }
        }
    }
}
