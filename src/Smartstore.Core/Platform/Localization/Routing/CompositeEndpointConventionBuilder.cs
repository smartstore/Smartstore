using System;
using Smartstore;

namespace Microsoft.AspNetCore.Builder
{
    internal sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder[] _conventions;

        public CompositeEndpointConventionBuilder(params IEndpointConventionBuilder[] conventions)
        {
            _conventions = conventions;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _conventions.Each(x => x.Add(convention));
        }
    }
}
