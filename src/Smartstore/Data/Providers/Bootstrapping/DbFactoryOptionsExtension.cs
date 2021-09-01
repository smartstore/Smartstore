using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;

namespace Smartstore.Data.Providers
{
    public class DbFactoryOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public DbFactoryOptionsExtension(DbContextOptions options = null)
        {
            var appConfig = EngineContext.Current.Application.Services.ResolveOptional<SmartConfiguration>();
            if (appConfig?.DbCommandTimeout != null)
            {
                CommandTimeout = appConfig.DbCommandTimeout.Value;
            }
        }

        protected DbFactoryOptionsExtension(DbFactoryOptionsExtension copyFrom)
        {
            Guard.NotNull(copyFrom, nameof(copyFrom));

            CommandTimeout = copyFrom.CommandTimeout;
            MinBatchSize = copyFrom.MinBatchSize;
            MaxBatchSize = copyFrom.MaxBatchSize;
            UseRelationalNulls = copyFrom.UseRelationalNulls;
            QuerySplittingBehavior = copyFrom.QuerySplittingBehavior;
        }

        public DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected DbFactoryOptionsExtension Clone()
            => new(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<IMethodCallTranslatorPlugin, DbFactoryMethodCallTranslatorPlugin>();
        }

        public void Validate(IDbContextOptions options)
        {
            // Nothing to validate
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

        public string MigrationsAssembly { get; private set; }
        public DbFactoryOptionsExtension WithMigrationsAssembly(string migrationsAssembly)
        {
            var clone = Clone();
            clone.MigrationsAssembly = migrationsAssembly;
            return clone;
        }

        #endregion

        #region Nested ExtensionInfo

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long? _serviceProviderHash;
            private string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider 
                => false;

            private new DbFactoryOptionsExtension Extension
                => (DbFactoryOptionsExtension)base.Extension;

            public override long GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = new HashCode();
                    hashCode.Add(Extension.CommandTimeout);
                    hashCode.Add(Extension.MinBatchSize);
                    hashCode.Add(Extension.MaxBatchSize);
                    hashCode.Add(Extension.UseRelationalNulls);
                    hashCode.Add(Extension.QuerySplittingBehavior);
                    hashCode.Add(Extension.MigrationsAssembly);

                    _serviceProviderHash = hashCode.ToHashCode();
                }

                return _serviceProviderHash.Value;
            }

            public override string LogFragment => $"Using '{nameof(DbFactoryOptionsExtension)}'";

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }
        }

        #endregion
    }
}