using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Customers;
using Smartstore.Data;
using Smartstore.Data.Hooks;

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
        public SmartDbContext(DbContextOptions options)
            : base(options)
        {
        }

        //public IDbQueryFilters<SmartDbContext> QueryFilters
        //{
        //    get => new DbQueryFilters<SmartDbContext>(this);
        //}

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
