namespace Microsoft.AspNetCore.Builder
{
    internal sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder[] _builders;

        public CompositeEndpointConventionBuilder(params IEndpointConventionBuilder[] builders)
        {
            _builders = builders;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            for (int i = 0; i < _builders.Length; i++)
            {
                _builders[i].Add(convention);
            }
        }
    }
}
