using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Installation;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using Smartstore.Threading;

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

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            // For installation only:
            // The connection string may change during installation attempts. 
            // Refresh the connection string in the underlying factory in that case.

            if (!builder.IsConfigured || DataSettings.DatabaseIsInstalled())
            {
                return;
            }

            var attemptedConString = DataSettings.Instance.ConnectionString;
            if (attemptedConString.IsEmpty())
            {
                return;
            }

            var extension = builder.Options.FindExtension<DbFactoryOptionsExtension>();
            if (extension == null)
            {
                return;
            }

            var currentConString = extension.ConnectionString;
            var currentCollation = extension.Collation;
            var attemptedCollation = DataSettings.Instance.Collation;

            if (currentConString == null)
            {
                // No database creation attempt yet
                ChangeConnectionString(attemptedConString, attemptedCollation);
           }
            else
            {
                // At least one database creation attempt
                if (attemptedConString != currentConString || attemptedCollation != currentCollation)
                {
                    // ConString changed. Refresh!
                    ChangeConnectionString(attemptedConString, attemptedCollation);
                }

                DataSettings.Instance.DbFactory?.ConfigureDbContext(builder, attemptedConString);
            }

            void ChangeConnectionString(string conString, string collation)
            {
                extension.ConnectionString = conString;
                extension.Collation = collation.NullEmpty();
                
                ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
            }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            DataSettings.Instance.DbFactory?.ConfigureModelConventions(configurationBuilder);
            base.ConfigureConventions(configurationBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DataSettings.Instance.DbFactory?.CreateModel(modelBuilder);

            var options = Options.FindExtension<DbFactoryOptionsExtension>();
            
            if (options.DefaultSchema.HasValue())
            {
                modelBuilder.HasDefaultSchema(options.DefaultSchema);
            }

            if (options.Collation.HasValue())
            {
                modelBuilder.UseCollation(options.Collation);
            }

            CreateModel(modelBuilder, options.ModelAssemblies);

            base.OnModelCreating(modelBuilder);
        }
    }
}
