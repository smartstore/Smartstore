using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;

namespace Smartstore.Data.Providers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Must fix ServicePropertyDiscoveryConvention")]
    public class FixedRuntimeConventionSetBuilder : RuntimeConventionSetBuilder
    {
        public FixedRuntimeConventionSetBuilder(
            IProviderConventionSetBuilder providerConventionSetBuilder, 
            IEnumerable<IConventionSetPlugin> plugins)
            : base(providerConventionSetBuilder, plugins)
        {
        }

        public override ConventionSet CreateConventionSet()
        {
            // EF Core 5 is buggy when it comes to discovering protected service properties in base types.
            // The default "ServicePropertyDiscoveryConvention" complains about duplicate properties, although
            // we have only one ILazyLoader property in BaseEntity. EF is not capable of discovering the hierarchy chain.
            // In EF Core 6 (11/2021) this will be fixed, but we cannot wait until then. Therefore we remove
            // "ServicePropertyDiscoveryConvention" and apply own conventions in HookingDbContext class.

            // TODO: (core) Remove FixedRuntimeConventionSetBuilder class when EF Core 6 is available (11/2021).

            var conventionSet = base.CreateConventionSet();
            ConventionSet.Remove(conventionSet.EntityTypeAddedConventions, typeof(ServicePropertyDiscoveryConvention));

            return conventionSet;
        }
    }
}
