using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    public class DefaultConditionalFilter<TFilter> : ConditionalFilter<TFilter> where TFilter : IFilterMetadata
    {
        private readonly Func<ActionContext, bool> _condition;

        public DefaultConditionalFilter(Func<ActionContext, bool> condition)
        {
            _condition = Guard.NotNull(condition, nameof(condition));
        }

        public override bool Match(ActionContext context)
            => _condition(context);
    }

    public abstract class ConditionalFilter<TFilter> : IOrderedFilter, IFilterConstraint
        where TFilter : IFilterMetadata
    {
        private ObjectFactory _factory;

        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public bool IsReusable { get; set; }

        /// <summary>
        /// Gets or sets the non-service arguments to pass to the <typeparamref name="TFilter"/> constructor.
        /// </summary>
        /// <remarks>
        /// Service arguments are found in the dependency injection container i.e. this filter supports constructor
        /// injection in addition to passing the given <see cref="Arguments"/>.
        /// </remarks>
        public object[] Arguments { get; set; }

        public abstract bool Match(ActionContext context);

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            Guard.NotNull(serviceProvider);

            if (_factory == null)
            {
                var argumentTypes = Arguments?.Select(a => a.GetType())?.ToArray();
                _factory = ActivatorUtilities.CreateFactory(typeof(TFilter), argumentTypes ?? Type.EmptyTypes);
            }

            var filter = (IFilterMetadata)_factory(serviceProvider, Arguments);
            if (filter is IFilterFactory filterFactory)
            {
                // Unwrap filter factories
                filter = filterFactory.CreateInstance(serviceProvider);
            }

            return filter;
        }
    }
}
