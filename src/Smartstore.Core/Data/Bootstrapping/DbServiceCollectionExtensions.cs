using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    public static class DbServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a scoped <see cref="DbQuerySettings" /> factory.
        /// </summary>
        public static IServiceCollection AddDbQuerySettings(this IServiceCollection services)
        {
            services.TryAddScoped<DbQuerySettings>(c => 
            {
                var storeContext = c.GetService<IStoreContext>();
                var aclService = c.GetService<IAclService>();

                return new DbQuerySettings(
                    aclService != null && !aclService.HasActiveAcl(),
                    storeContext?.IsSingleStoreMode() ?? false);
            });

            return services;
        }

        /// <summary>
        /// Registers the open generic <see cref="DbMigrator{TContext}" /> as transient dependency.
        /// </summary>
        public static IServiceCollection AddDbMigrator(this IServiceCollection services, IApplicationContext appContext)
        {
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
            services.AddTransient(typeof(DbMigrator<>));

            // Fluent migrator.
            var migrationAssemblies = appContext.TypeScanner.FindTypes<MigrationBase>()
                .Select(x => x.Assembly)
                .Where(x => !x.FullName.Contains("FluentMigrator.Runner"))
                .Distinct()
                .ToArray();
            //$"assemblies {string.Join(", ", migrationAssemblies.Select(x => x.GetName().Name))}".Dump();

            var migrationTimeout = appContext.AppConfiguration.DbMigrationCommandTimeout ?? 60;

            services
                .AddFluentMigratorCore()
                .AddScoped<IProcessorAccessor, MigrationProcessorAccessor>()
                .AddScoped<IDbMigrator2, DbMigrator2>()
                //.AddSingleton<IConventionSet, MigrationConventionSet>()
                //.AddLogging(lb => lb.AddFluentMigratorConsole())
                //.Configure<RunnerOptions>(opt =>
                //{
                //    opt.Profile = "Development"   // Selectively apply migrations depending on whatever.
                //    opt.Tags = new[] { "UK", "Production" }   // Used to filter migrations by tags.
                //})
                .ConfigureRunner(rb => rb
                    .AddSqlServer()
                    .AddMySql5()
                    .WithVersionTable(new MigrationHistory())
                    .WithGlobalConnectionString(DataSettings.Instance.ConnectionString) // Isn't AddScoped<IConnectionStringAccessor> better?
                    .WithGlobalCommandTimeout(TimeSpan.FromSeconds(migrationTimeout))
                    .ScanIn(migrationAssemblies)
                        .For.Migrations()
                        .For.EmbeddedResources());

            return services;
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
        public static IServiceCollection AddPooledDbContextFactory<TContext>(
            this IServiceCollection services,
            Type contextImplType,
            int poolSize = 128,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            // INFO: TContextImpl cannot be a type parameter because type is defined in an assembly that is not referenced.
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(contextImplType, nameof(contextImplType));

            var addPoolingOptionsMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethod("AddPoolingOptions", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(contextImplType);

            // --> Call AddPoolingOptions<TContextImplementation>(services, optionsAction, poolSize)
            addPoolingOptionsMethod.Invoke(null, new object[] { services, optionsBuilder, poolSize });

            // --> Call services.TryAddSingleton<IDbContextPool<TContextImpl>, DbContextPool<TContextImpl>>()
            var contextPoolServiceType = typeof(IDbContextPool<>).MakeGenericType(contextImplType);
            var contextPoolImplType = typeof(DbContextPool<>).MakeGenericType(contextImplType);
            services.TryAddSingleton(contextPoolServiceType, contextPoolImplType);

            // --> Register provider-aware IDbContextFactory<TContext>
            services.TryAddSingleton(c =>
            {
                var pool = c.GetRequiredService(contextPoolServiceType);
                var pooledFactoryType = typeof(PooledApplicationDbContextFactory<,>).MakeGenericType(typeof(TContext), contextImplType);

                var instance = Activator.CreateInstance(pooledFactoryType, new object[] { pool });
                return (IDbContextFactory<TContext>)instance;
            });

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            DbMigrationManager.Instance.RegisterDbContext(typeof(TContext));

            return services;
        }
    }
}