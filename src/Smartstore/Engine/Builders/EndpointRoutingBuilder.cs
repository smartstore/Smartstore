using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Smartstore.Engine.Builders
{
    public sealed class EndpointRoutingBuilder
    {
        private readonly List<BuildAction> _buildActions = new();

        readonly struct BuildAction
        {
            public int Order { get; init; }
            public Action<IEndpointRouteBuilder> Action { get; init; }
        }

        public IApplicationBuilder ApplicationBuilder { get; init; }

        public IApplicationContext ApplicationContext { get; init; }

        public IEndpointRouteBuilder RouteBuilder { get; init; }

        public void MapRoutes(int order, Action<IEndpointRouteBuilder> buildAction)
        {
            Guard.NotNull(buildAction, nameof(buildAction));

            _buildActions.Add(new BuildAction { Order = order, Action = buildAction });
        }

        internal void Build(IEndpointRouteBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            foreach (var buildAction in _buildActions.OrderBy(x => x.Order))
            {
                buildAction.Action(builder);
            }
        }
    }
}