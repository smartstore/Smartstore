using System.Globalization;
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
            => new(this);

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

        public bool EnableLogging { get; private set; } = true;
        /// <summary>
        /// Should the debug level logging be enabled? Default is true.
        /// </summary>
        public CachingOptionsExtension WithUseLogging(bool useLogging)
        {
            var clone = Clone();
            clone.EnableLogging = useLogging;
            return clone;
        }

        public TimeSpan DefaultExpirationTimeout { get; private set; } = TimeSpan.FromHours(3);
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

        public int DefaultMaxRows { get; private set; } = 1000;
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

        #endregion

        #region Nested ExtensionInfo

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            private long? _serviceProviderHash;

            public ExtensionInfo(CachingOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider
                => false;

            private new CachingOptionsExtension Extension
                => (CachingOptionsExtension)base.Extension;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            {
                return other is ExtensionInfo otherInfo
                    && Extension.EnableLogging == otherInfo.Extension.EnableLogging
                    && Extension.DefaultExpirationTimeout == otherInfo.Extension.DefaultExpirationTimeout
                    && Extension.DefaultMaxRows == otherInfo.Extension.DefaultMaxRows;
            }

            public override int GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = new HashCode();
                    hashCode.Add(Extension.EnableLogging);
                    hashCode.Add(Extension.DefaultExpirationTimeout);
                    hashCode.Add(Extension.DefaultMaxRows);

                    _serviceProviderHash = hashCode.ToHashCode();
                }

                return _serviceProviderHash.Value.Convert<int>();
            }

            public override string LogFragment
                => $"Using '{nameof(CachingOptionsExtension)}'";

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["Smartstore.Data.Caching:" + nameof(Extension.EnableLogging)] = HashCode.Combine(Extension.EnableLogging).ToString(CultureInfo.InvariantCulture);
                debugInfo["Smartstore.Data.Caching:" + nameof(Extension.DefaultExpirationTimeout)] = HashCode.Combine(Extension.DefaultExpirationTimeout).ToString(CultureInfo.InvariantCulture);
                debugInfo["Smartstore.Data.Caching:" + nameof(Extension.DefaultMaxRows)] = HashCode.Combine(Extension.DefaultMaxRows).ToString(CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }
}