using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Engine;

namespace Smartstore.Core.Data
{
    public abstract class AsyncDbSaveHook<TEntity> : AsyncDbSaveHook<SmartDbContext, TEntity>
        where TEntity : class
    {
    }

    public abstract class DbSaveHook<TEntity> : DbSaveHook<SmartDbContext, TEntity>
        where TEntity : class
    {
    }

    public partial class SmartDbContext : HookingDbContext
    {
        /// <summary>
        /// The name for the Migrations history table.
        /// </summary>
        public const string MigrationHistoryTableName = "__EFMigrationsHistory_Core";

        public SmartDbContext(DbContextOptions<SmartDbContext> options)
            : base(options)
        {
        }

        protected SmartDbContext(DbContextOptions options)
            : base(options)
        {
        }

        [SuppressMessage("Performance", "CA1822:Member can be static", Justification = "Seriously?")]
        public DbQuerySettings QuerySettings
        {
            get => EngineContext.Current.Scope.ResolveOptional<DbQuerySettings>() ?? DbQuerySettings.Default;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ???
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CreateModel(
                modelBuilder,
                // Contains all entities
                typeof(SmartDbContext).Assembly,
                // Contains provider specific entity configurations
                DataSettings.Instance.DbFactory.GetType().Assembly); 

            base.OnModelCreating(modelBuilder);
        }
    }
}
