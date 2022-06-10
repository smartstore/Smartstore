using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Migrations;

namespace Smartstore.Data.Providers
{
    public class DbFactoryDbContextOptionsBuilder : IHideObjectMembers
    {
        public DbFactoryDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        {
            OptionsBuilder = Guard.NotNull(optionsBuilder, nameof(optionsBuilder));
        }

        /// <summary>
        /// Gets the core options builder.
        /// </summary>
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        /// <summary>
        /// Sets an option by cloning the extension used to store the settings. This ensures the builder
        /// does not modify options that are already in use elsewhere.
        /// </summary>
        /// <param name="setAction">An action to set the option.</param>
        /// <returns> The same builder instance so that multiple calls can be chained.</returns>
        protected virtual DbFactoryDbContextOptionsBuilder WithOption(Func<DbFactoryOptionsExtension, DbFactoryOptionsExtension> setAction)
        {
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
                setAction(OptionsBuilder.Options.FindExtension<DbFactoryOptionsExtension>() ?? new DbFactoryOptionsExtension(OptionsBuilder.Options)));

            return this;
        }

        /// <summary>
        /// Configures the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
        /// </summary>
        /// <param name="commandTimeout">The time in seconds to wait for the command to execute.</param>
        public virtual DbFactoryDbContextOptionsBuilder CommandTimeout(int? commandTimeout)
            => WithOption(e => e.WithCommandTimeout(commandTimeout));

        /// <summary>
        /// Configures the minimum number of statements that are needed for a multi-statement command sent to the database during <see cref="DbContext.SaveChanges()" />.
        /// </summary>
        /// <param name="minBatchSize">The minimum number of statements.</param>
        public virtual DbFactoryDbContextOptionsBuilder MinBatchSize(int minBatchSize)
            => WithOption(e => e.WithMinBatchSize(minBatchSize));

        /// <summary>
        /// Configures the maximum number of statements that will be included in commands sent to the database during <see cref="DbContext.SaveChanges()" />.
        /// </summary>
        /// <param name="maxBatchSize">The maximum number of statements.</param>
        public virtual DbFactoryDbContextOptionsBuilder MaxBatchSize(int maxBatchSize)
            => WithOption(e => e.WithMaxBatchSize(maxBatchSize));

        /// <summary>
        /// Configures the context to use relational database semantics when comparing null values. By default,
        /// Entity Framework will use C# semantics for null values, and generate SQL to compensate for differences
        /// in how the database handles nulls.
        /// </summary>
        public virtual DbFactoryDbContextOptionsBuilder UseRelationalNulls(bool useRelationalNulls = true)
            => WithOption(e => e.WithUseRelationalNulls(useRelationalNulls));

        /// <summary>
        /// Configures the <see cref="QuerySplittingBehavior" /> to use when loading related collections in a query.
        /// </summary>
        public virtual DbFactoryDbContextOptionsBuilder QuerySplittingBehavior(QuerySplittingBehavior querySplittingBehavior)
            => WithOption(e => e.WithQuerySplittingBehavior(querySplittingBehavior));

        /// <summary>
        /// Configures an assembly where entity models and migrations are maintained for this context.
        /// </summary>
        /// <param name="assembly">The assembly to add.</param>
        public virtual DbFactoryDbContextOptionsBuilder AddModelAssembly(Assembly assembly)
            => WithOption(e => e.WithModelAssemblies(new[] { Guard.NotNull(assembly, nameof(assembly)) }));

        /// <summary>
        /// Configures the assemblies where entity models and migrations are maintained for this context.
        /// </summary>
        /// <param name="assemblies">The assemblies to add.</param>
        public virtual DbFactoryDbContextOptionsBuilder AddModelAssemblies(IEnumerable<Assembly> assemblies)
            => WithOption(e => e.WithModelAssemblies(Guard.NotNull(assemblies, nameof(assemblies))));

        /// <summary>
        /// Adds a global data seeder to the context. Global seeders are instantiated and executed on every application startup.
        /// The seeder type must be compatible with the configured <see cref="DbContext"/> type, otherwise an exception is raised.
        /// </summary>
        /// <typeparam name="TSeeder">Concrete type of data seeder to add</typeparam>
        public virtual DbFactoryDbContextOptionsBuilder AddDataSeeder<TContext, TSeeder>()
            where TContext : HookingDbContext
            where TSeeder : IDataSeeder<TContext>, new()
            => WithOption(e => e.WithDataSeeder<TContext, TSeeder>());
    }
}
