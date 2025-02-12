using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Web.Filters
{
    public class DefaultConditionalFilter<TFilter> : ConditionalFilter where TFilter : IFilterMetadata
    {
        private readonly Func<ActionContext, bool> _condition;

        public DefaultConditionalFilter(Func<ActionContext, bool> condition) 
            : base(typeof(TFilter))
        {
            _condition = Guard.NotNull(condition);
        }

        public override bool Match(ActionContext context)
            => _condition(context);
    }

    public abstract class ConditionalFilter : IOrderedFilter, IFilterConstraint
    {
        private ObjectFactory _factory;

        protected ConditionalFilter(Type filterType)
        {
            Guard.NotNull(filterType);
            Guard.IsAssignableFrom<IFilterMetadata>(filterType);

            FilterType = filterType;
        }

        public Type FilterType { get;  }

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
                _factory = ActivatorUtilities.CreateFactory(FilterType, argumentTypes ?? Type.EmptyTypes);
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
