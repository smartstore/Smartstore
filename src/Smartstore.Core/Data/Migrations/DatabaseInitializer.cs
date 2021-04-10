using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Responsible for initializing all migratable <see cref="DbContext"/> instances on app startup.
    /// </summary>
    public interface IDatabaseInitializer
    {
        /// <summary>
        /// Initializes / migrates all migratable <see cref="DbContext"/> instances.
        /// </summary>
        Task InitializeDatabasesAsync();
    }
    
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private static readonly SyncedCollection<Type> _initializedContextTypes = new List<Type>().AsSynchronized();
        private readonly ILifetimeScope _scope;

        public DatabaseInitializer(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public virtual async Task InitializeDatabasesAsync()
        {
            var migratableDbContextTypes = DbMigrationManager.Instance.GetMigratableDbContextTypes();

            foreach (var dbContextType in migratableDbContextTypes)
            {
                var migrator = _scope.Resolve(typeof(DbMigrator<>).MakeGenericType(dbContextType)) as DbMigrator;
                await InitializeDatabaseAsync(migrator);
            }
        }

        protected virtual async Task InitializeDatabaseAsync(DbMigrator migrator)
        {
            Guard.NotNull(migrator, nameof(migrator));

            var context = migrator.Context;
            var type = context.GetInvariantType();

            if (_initializedContextTypes.Contains(type))
            {
                return;
            }

            if (!await context.Database.CanConnectAsync())
            {
                throw new InvalidOperationException($"Database migration failed because the target database does not exist. Ensure the database was initialized and properly seeded with data.");
            }

            using (new DbContextScope(context, minHookImportance: HookImportance.Essential))
            {
                // run all pending migrations
                var appliedCount = await migrator.RunPendingMigrationsAsync();

                if (appliedCount > 0)
                {
                    // Seed(context);
                }
                else
                {
                    // DB is up-to-date and no migration ran.
                    if (context is SmartDbContext db)
                    {
                        //// Call the main Seed method anyway (on every startup),
                        //// we could have locale resources or settings to add/update.
                        //coreConfig.SeedDatabase(ctx);
                    }
                }

                _initializedContextTypes.Add(type);
            }
        }
    }
}
