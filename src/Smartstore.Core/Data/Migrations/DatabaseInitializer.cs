using System.Reflection;
using Autofac;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private static readonly SyncedCollection<Type> _initializedContextTypes = new List<Type>().AsSynchronized();
        private readonly ILifetimeScope _scope;
        private readonly SmartConfiguration _appConfig;
        private readonly ITypeScanner _typeScanner;

        public DatabaseInitializer(
            ILifetimeScope scope,
            ITypeScanner typeScanner,
            SmartConfiguration appConfig)
        {
            _scope = scope;
            _appConfig = appConfig;
            _typeScanner = typeScanner;
        }

        public virtual async Task InitializeDatabasesAsync(CancellationToken cancelToken = default)
        {
            var contextTypes = GetDbContextTypes();
            foreach (var contextType in contextTypes)
            {
                await InitializeDatabaseAsync(contextType, cancelToken);
            }
        }

        protected virtual async Task InitializeDatabaseAsync(Type dbContextType, CancellationToken cancelToken = default)
        {
            Guard.NotNull(dbContextType);
            Guard.IsAssignableFrom<HookingDbContext>(dbContextType);

            var migrator = _scope.Resolve(typeof(DbMigrator<>).MakeGenericType(dbContextType)) as DbMigrator;

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

            using (BeginMigration(context))
            {
                // Run all pending migrations
                await migrator.RunPendingMigrationsAsync(null, cancelToken);

                // Execute the global seeders anyway (on every startup),
                // we could have locale resources or settings to add/update.
                await RunGlobalSeeders(context, cancelToken);

                _initializedContextTypes.Add(type);
            }
        }

        public virtual async Task RunPendingSeedersAsync(CancellationToken cancelToken = default)
        {
            var contextTypes = GetDbContextTypes();
            foreach (var contextType in contextTypes)
            {
                await RunPendingSeedersAsync(contextType, cancelToken);
            }
        }

        protected async Task RunPendingSeedersAsync(Type dbContextType, CancellationToken cancelToken = default)
        {
            Guard.NotNull(dbContextType);
            Guard.IsAssignableFrom<HookingDbContext>(dbContextType);

            var migrator = _scope.Resolve(typeof(DbMigrator<>).MakeGenericType(dbContextType)) as DbMigrator;

            using (BeginMigration(migrator.Context))
            {
                await migrator.RunLateSeedersAsync(cancelToken);
            }
        }

        private static async Task RunGlobalSeeders(HookingDbContext dbContext, CancellationToken cancelToken = default)
        {
            var seederTypes = dbContext.Options.FindExtension<DbFactoryOptionsExtension>()?.DataSeederTypes;

            if (seederTypes != null)
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
        }

        /// <summary>
        /// Resolves/discovers all migratable data context types.
        /// </summary>
        private Type[] GetDbContextTypes()
        {
            if (!ModularState.Instance.HasChanged)
            {
                // (perf) Ignore modules, they did not change since last migration.
                return [typeof(SmartDbContext)];
            }
            else
            {
                return _typeScanner.FindTypes<HookingDbContext>().ToArray();
            }
        }

        private IDisposable BeginMigration(HookingDbContext context)
        {
            var prevCommandTimeout = context.Database.GetCommandTimeout();
            var dbScope = new DbContextScope(context, minHookImportance: HookImportance.Essential);

            // Set (usually longer) command timeout for migrations
            if (_appConfig.DbMigrationCommandTimeout.HasValue && _appConfig.DbMigrationCommandTimeout.Value > 15)
            {
                context.Database.SetCommandTimeout(_appConfig.DbMigrationCommandTimeout.Value);
            }

            return new ActionDisposable(() =>
            {
                // Restore standard command timeout
                context.Database.SetCommandTimeout(prevCommandTimeout);

                dbScope.Dispose();
            });
        }
    }
}
