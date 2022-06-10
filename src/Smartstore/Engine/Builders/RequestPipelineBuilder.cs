using Microsoft.AspNetCore.Builder;

namespace Smartstore.Engine
{
    public sealed class RequestPipelineBuilder : IHideObjectMembers
    {
        private readonly List<BuildAction> _buildActions = new();

        readonly struct BuildAction
        {
            public int Order { get; init; }
            public Action<IApplicationBuilder> Action { get; init; }
        }

        public IApplicationBuilder ApplicationBuilder { get; init; }

        public IApplicationContext ApplicationContext { get; init; }

        public void Configure(int order, Action<IApplicationBuilder> buildAction)
        {
            Guard.NotNull(buildAction, nameof(buildAction));

            _buildActions.Add(new BuildAction { Order = order, Action = buildAction });
        }

        internal void Build(IApplicationBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder));

            foreach (var buildAction in _buildActions.OrderBy(x => x.Order))
            {
                buildAction.Action(builder);
            }
        }
    }
}
