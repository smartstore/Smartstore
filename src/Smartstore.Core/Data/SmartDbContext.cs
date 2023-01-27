using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Autofac;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using FluentMigrator.Runner.Conventions;
using System.Reflection;
using Smartstore.Core.Data.Migrations;

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
            if (currentConString == null)
            {
                ChangeConnectionString(attemptedConString);
            }
            else
            {
                if (attemptedConString != currentConString)
                {
                    // ConString changed. Refresh!
                    ChangeConnectionString(attemptedConString);
                }

                DataSettings.Instance.DbFactory?.ConfigureDbContext(builder, attemptedConString);
            }

            void ChangeConnectionString(string value)
            {
                extension.ConnectionString = value;
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

            var options = this.Options.FindExtension<DbFactoryOptionsExtension>();
            
            if (options.DefaultSchema.HasValue())
            {
                modelBuilder.HasDefaultSchema(options.DefaultSchema);
            }

            CreateModel(modelBuilder, options.ModelAssemblies);

            ConfigureMigratorConventions(options.ModelAssemblies);

            base.OnModelCreating(modelBuilder);
        }

        protected virtual void ConfigureMigratorConventions(IEnumerable<Assembly> assemblies)
        {
            var appContext = EngineContext.Current?.Application;
            var conventionSet = appContext?.Services?.ResolveOptional<IConventionSet>();
            if (conventionSet != null)
            {
                var typeScanner = appContext.TypeScanner;
                var conventionProviders = typeScanner
                    .FindTypes<IConventionProvider>(assemblies)
                    .Select(Activator.CreateInstance)
                    .Cast<IConventionProvider>();

                foreach (var provider in conventionProviders)
                {
                    provider.Configure(conventionSet);
                }
            }
        }
    }
}
