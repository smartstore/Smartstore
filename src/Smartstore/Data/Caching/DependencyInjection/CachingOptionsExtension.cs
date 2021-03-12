using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Smartstore.Data.Caching
{
    /// <summary>
    ///     <para>
    ///         Represents 2nd level caching options managed.
    ///         These options are set using <see cref="CachingDbContextOptionsBuilder" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are designed to be immutable. To change an option, call one of the 'With...'
    ///         methods to obtain a new instance with the option changed.
    ///     </para>
    /// </summary>
    public class CachingOptionsExtension : IDbContextOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public CachingOptionsExtension()
        {
        }

        protected CachingOptionsExtension(CachingOptionsExtension copyFrom)
        {
            Guard.NotNull(copyFrom, nameof(copyFrom));

            EnableLogging = copyFrom.EnableLogging;
            DefaultExpirationTimeout = copyFrom.DefaultExpirationTimeout;
            DefaultMaxRows = copyFrom.DefaultMaxRows;
        }

        protected CachingOptionsExtension Clone()
            => new CachingOptionsExtension(this);

        #region IDbContextOptionsExtension

        public DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        public void ApplyServices(IServiceCollection services)
        {
            services.TryAddSingleton<IDbCache, DbCache>();
            services.TryAddScoped<IQueryKeyGenerator, QueryKeyGenerator>();
        }

        public void Validate(IDbContextOptions options)
        {
        }

        #endregion

        #region Options

        /// <summary>
        /// Should the debug level logging be enabled? Default is true.
        /// </summary>
        public CachingOptionsExtension WithUseLogging(bool useLogging)
        {
            var clone = Clone();
            clone.EnableLogging = useLogging;
            return clone;
        }

        /// <summary>
        /// The default (fallback) expiration timeout for cacheable entities.
        /// Default is 3 hours.
        /// </summary>
        public CachingOptionsExtension WithDefaultExpirationTimeout(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentException($"Invalid caching timeout {timeout}", nameof(timeout));
            }

            var clone = Clone();
            clone.DefaultExpirationTimeout = timeout;
            return clone;
        }

        /// <summary>
        /// The default (fallback) max rows limit.
        /// Default is 1000 rows.
        /// </summary>
        public CachingOptionsExtension WithDefaultMaxRows(int maxRows)
        {
            if (maxRows <= 0)
            {
                throw new ArgumentException($"Invalid caching MaxRows parameter {maxRows}", nameof(maxRows));
            }

            var clone = Clone();
            clone.DefaultMaxRows = maxRows;
            return clone;
        }

        public bool EnableLogging { set; get; } = true;

        public TimeSpan DefaultExpirationTimeout { get; set; } = TimeSpan.FromHours(3);

        public int DefaultMaxRows { get; set; } = 1000;

        #endregion

        #region Nested ExtensionInfo

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(CachingOptionsExtension extension)
                : base(extension)
            {
            }

            private new CachingOptionsExtension Extension => (CachingOptionsExtension)base.Extension;

            public override long GetServiceProviderHashCode() => 0L;

            public override bool IsDatabaseProvider => true;

            // TODO: (core) What to return as LogFragment?
            public override string LogFragment => $"Using '{nameof(CachingOptionsExtension)}'";

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
            }
        }

        #endregion
    }
}