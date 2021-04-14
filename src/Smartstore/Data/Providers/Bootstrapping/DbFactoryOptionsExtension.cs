using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Data.Providers
{
    public class DbFactoryOptionsExtension : RelationalOptionsExtension
    {
        private DbContextOptionsExtensionInfo _info;

        public DbFactoryOptionsExtension()
        {
            //
        }

        protected DbFactoryOptionsExtension(DbFactoryOptionsExtension copyFrom)
            : base(copyFrom)
        {
            Guard.NotNull(copyFrom, nameof(copyFrom));

            //
        }

        public override DbContextOptionsExtensionInfo Info
            => _info ??= new ExtensionInfo(this);

        protected override RelationalOptionsExtension Clone()
            => new DbFactoryOptionsExtension(this);

        public override void ApplyServices(IServiceCollection services)
        {
            // No services
        }

        #region Configuration

        public bool Something { set; private get; }

        public DbFactoryOptionsExtension WithSomething(bool something)
        {
            var clone = (DbFactoryOptionsExtension)Clone();
            clone.Something = something;
            return clone;
        }

        #endregion

        #region Nested ExtensionInfo

        private sealed class ExtensionInfo : RelationalExtensionInfo
        {
            private long? _serviceProviderHash;
            private string _logFragment;

            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            private new DbFactoryOptionsExtension Extension 
                => (DbFactoryOptionsExtension)base.Extension;

            public override bool IsDatabaseProvider => false;

            public override long GetServiceProviderHashCode()
            {
                if (_serviceProviderHash == null)
                {
                    var hashCode = new HashCode();
                    hashCode.Add(Extension.Something);

                    _serviceProviderHash = hashCode.ToHashCode();
                }

                return _serviceProviderHash.Value;
            }

            public override string LogFragment
            {
                get
                {
                    if (_logFragment == null)
                    {
                        var builder = new StringBuilder();

                        builder.Append(base.LogFragment);

                        //if (Extension.ServerVersion != null)
                        //{
                        //    builder.Append("ServerVersion ")
                        //        .Append(Extension.ServerVersion)
                        //        .Append(" ");
                        //}

                        _logFragment = builder.ToString();
                    }

                    return _logFragment;
                }
            }

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["Smartstore.Data.Providers:" + nameof(Extension.Something)] = HashCode.Combine(Extension.Something).ToString(CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }
}