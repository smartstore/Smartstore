using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Design;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;

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

    [CheckTables("Customer", "Discount", "Order", "Product", "ShoppingCartItem", "QueuedEmailAttachment", "ExportProfile")]
    public partial class SmartDbContext : HookingDbContext
    {
        public SmartDbContext(DbContextOptions options)
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
            var options = this.Options.FindExtension<DbFactoryOptionsExtension>();

            CreateModel(modelBuilder, options.ModelAssemblies); 

            base.OnModelCreating(modelBuilder);
        }
    }
    public class SmartDbContextFactory : IDesignTimeDbContextFactory<SmartDbContext>
    {
        public SmartDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SmartDbContext>()
               .UseDbFactory(b =>
               {
                   b.AddModelAssembly(this.GetType().Assembly);
               });
            return new SmartDbContext(optionsBuilder.Options);
        }
    }
}
