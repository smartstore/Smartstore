using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using Smartstore.Engine;

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
        private readonly SmartConfiguration _appConfig;

        public DatabaseInitializer(ILifetimeScope scope, SmartConfiguration appConfig)
        {
            _scope = scope;
            _appConfig = appConfig;
        }

        public virtual async Task InitializeDatabasesAsync()
        {
            var migratableDbContextTypes = DbMigrationManager.Instance.GetDbContextTypes();

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
                // Set (usually longer) command timeout for migrations
                var prevCommandTimeout = context.Database.GetCommandTimeout();
                if (_appConfig.DbMigrationCommandTimeout.HasValue && _appConfig.DbMigrationCommandTimeout.Value > 15)
                {
                    context.Database.SetCommandTimeout(_appConfig.DbMigrationCommandTimeout.Value);
                }
                
                // Run all pending migrations
                await migrator.RunPendingMigrationsAsync();
                
                // Call the global Seed method anyway (on every startup),
                // we could have locale resources or settings to add/update.
                await GlobalSeed(migrator);

                // Restore standard command timeout
                context.Database.SetCommandTimeout(prevCommandTimeout);

                _initializedContextTypes.Add(type);
            }
        }

        private static async Task GlobalSeed(DbMigrator migrator)
        {
            var extension = migrator.Context.Options?.FindExtension<DbFactoryOptionsExtension>();
            if (extension?.DataSeederType != null)
            {
                var seeder = Activator.CreateInstance(extension.DataSeederType);
                if (seeder != null)
                {
                    var seedMethod = extension.DataSeederType.GetMethod(nameof(IDataSeeder<HookingDbContext>.SeedAsync), BindingFlags.Public | BindingFlags.Instance);
                    if (seedMethod != null)
                    {
                        await (Task)seedMethod.Invoke(seeder, new object[] { migrator.Context });
                        await migrator.Context.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
