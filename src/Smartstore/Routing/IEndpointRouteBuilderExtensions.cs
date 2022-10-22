using Microsoft.AspNetCore.Routing;
using Smartstore;

namespace Microsoft.AspNetCore.Builder
{
    public static class IEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps multiple endpoints and groups them in a composite builder to simplify adding conventions and metadata.
        /// </summary>
        public static IEndpointConventionBuilder MapComposite(this IEndpointRouteBuilder endpoints, params IEndpointConventionBuilder[] builders)
        {
            Guard.NotNull(endpoints, nameof(endpoints));
            Guard.NotNull(builders, nameof(builders));

            if (builders.Length == 1)
            {
                return builders[0];
            }

            return new CompositeEndpointConventionBuilder(builders);
        }
    }
}
