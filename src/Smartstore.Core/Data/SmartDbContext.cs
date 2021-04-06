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
            CreateModel(modelBuilder, typeof(SmartDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
