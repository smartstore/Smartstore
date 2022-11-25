#nullable enable

using System.Reflection;
using AngleSharp.Common;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Migrations;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    public class DbFactoryOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo? _info;
        private DbContextOptions _options;

        public DbFactoryOptionsExtension(DbContextOptions options)
        {
            _options = options;
            
            var appConfig = EngineContext.Current.Application.Services.ResolveOptional<SmartConfiguration>();
            CommandTimeout = appConfig?.DbCommandTimeout;
            DefaultSchema = appConfig?.DbDefaultSchema.NullEmpty();
        }

        protected DbFactoryOptionsExtension(DbFactoryOptionsExtension copyFrom)
        {
            Guard.NotNull(copyFrom, nameof(copyFrom));

            _options = copyFrom._options;

            CommandTimeout = copyFrom.CommandTimeout;
            MinBatchSize = copyFrom.MinBatchSize;
            MaxBatchSize = copyFrom.MaxBatchSize;
            UseRelationalNulls = copyFrom.UseRelationalNulls;
            QuerySplittingBehavior = copyFrom.QuerySplittingBehavior;
            ModelAssemblies = copyFrom.ModelAssemblies;
            DataSeederTypes = copyFrom.DataSeederTypes;
        }

        public DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected DbFactoryOptionsExtension Clone()
            => new(this);

        public void ApplyServices(IServiceCollection services)
        {
        }

        public void Validate(IDbContextOptions options)
        {
            if (ModelAssemblies == null || !ModelAssemblies.Any())
            {
                throw new InvalidOperationException("At least one assembly containing entity models and migrations must be specified.");
            }
        }

        #region Options

        public int? CommandTimeout { get; private set; }
        public DbFactoryOptionsExtension WithCommandTimeout(int? commandTimeout)
        {
            if (commandTimeout.HasValue)
            {
                Guard.IsPositive(commandTimeout.Value, nameof(commandTimeout));
            }

            var clone = Clone();
            clone.CommandTimeout = commandTimeout;
            return clone;
        }

        public int? MinBatchSize { get; private set; }
        public DbFactoryOptionsExtension WithMinBatchSize(int? minBatchSize)
        {
            if (minBatchSize.HasValue)
            {
                Guard.IsPositive(minBatchSize.Value, nameof(minBatchSize));
            }

            var clone = Clone();
            clone.MinBatchSize = minBatchSize;
            return clone;
        }

        public int? MaxBatchSize { get; private set; }
        public DbFactoryOptionsExtension WithMaxBatchSize(int? maxBatchSize)
        {
            if (maxBatchSize.HasValue)
            {
                Guard.IsPositive(maxBatchSize.Value, nameof(maxBatchSize));
            }

            var clone = Clone();
            clone.MaxBatchSize = maxBatchSize;
            return clone;
        }

        public bool? UseRelationalNulls { get; private set; }
        public DbFactoryOptionsExtension WithUseRelationalNulls(bool useRelationalNulls)
        {
            var clone = Clone();
            clone.UseRelationalNulls = useRelationalNulls;
            return clone;
        }

        public QuerySplittingBehavior? QuerySplittingBehavior { get; private set; }
        public DbFactoryOptionsExtension WithQuerySplittingBehavior(QuerySplittingBehavior querySplittingBehavior)
        {
            var clone = Clone();
            clone.QuerySplittingBehavior = querySplittingBehavior;
            return clone;
        }

        public string? DefaultSchema { get; private set; }
        public DbFactoryOptionsExtension WithDefaultSchema(string? schema)
        {
            var clone = Clone();
            clone.DefaultSchema = schema;
            return clone;
        }

        public IEnumerable<Assembly> ModelAssemblies { get; private set; } = default!;
        public DbFactoryOptionsExtension WithModelAssemblies(IEnumerable<Assembly> assemblies)
        {
            Guard.NotNull(assemblies, nameof(assemblies));

            var clone = Clone();
            clone.ModelAssemblies = ModelAssemblies == null
                ? assemblies
                : ModelAssemblies.Concat(assemblies);
            return clone;
        }

        public IEnumerable<Type> DataSeederTypes { get; private set; } = default!;
        public DbFactoryOptionsExtension WithDataSeeder<TContext, TSeeder>()
            where TContext : HookingDbContext
            where TSeeder : IDataSeeder<TContext>, new()
        {
            if (!_options.ContextType.IsAssignableFrom(typeof(TContext)))
            {
                throw new InvalidOperationException($"The data seeder '{typeof(TSeeder)}' is not compatible with the configured DbContext type '{_options.ContextType}'.");
            }

            var clone = Clone();
            clone.DataSeederTypes = (DataSeederTypes ?? Type.EmptyTypes).Concat(new[] { typeof(TSeeder) });
            return clone;
        }

        #endregion

        #region Nested ExtensionInfo

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long? _serviceProviderHash;

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider
                => false;

            private new DbFactoryOptionsExtension Extension
                => (DbFactoryOptionsExtension)base.Extension;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo? other)
            {
                return other is ExtensionInfo otherInfo
                    && Extension.CommandTimeout == otherInfo.Extension.CommandTimeout
                    && Extension.MinBatchSize == otherInfo.Extension.MinBatchSize
                    && Extension.MaxBatchSize == otherInfo.Extension.MaxBatchSize
                    && Extension.UseRelationalNulls == otherInfo.Extension.UseRelationalNulls
                    && Extension.QuerySplittingBehavior == otherInfo.Extension.QuerySplittingBehavior
                    && Extension.DefaultSchema == otherInfo.Extension.DefaultSchema
                    && (Extension.ModelAssemblies == otherInfo.Extension.ModelAssemblies || Extension.ModelAssemblies.SequenceEqual(otherInfo.Extension.ModelAssemblies))
                    && (Extension.DataSeederTypes == otherInfo.Extension.DataSeederTypes || Extension.DataSeederTypes.SequenceEqual(otherInfo.Extension.DataSeederTypes));
            }

            public override int GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = new HashCode();
                    hashCode.Add(Extension.CommandTimeout);
                    hashCode.Add(Extension.MinBatchSize);
                    hashCode.Add(Extension.MaxBatchSize);
                    hashCode.Add(Extension.UseRelationalNulls);
                    hashCode.Add(Extension.QuerySplittingBehavior);

                    if (Extension.DefaultSchema != null)
                    {
                        hashCode.Add(Extension.DefaultSchema);
                    }

                    if (Extension.ModelAssemblies != null)
                    {
                        Extension.ModelAssemblies.Each(x => hashCode.Add(x.GetHashCode()));
                    }

                    if (Extension.DataSeederTypes != null)
                    {
                        Extension.DataSeederTypes.Each(x => hashCode.Add(x.GetHashCode()));
                    }

                    _serviceProviderHash = hashCode.ToHashCode();
                }

                return _serviceProviderHash.Value.Convert<int>();
            }

            public override string LogFragment => $"Using '{nameof(DbFactoryOptionsExtension)}'";

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }
        }

        #endregion
    }
}