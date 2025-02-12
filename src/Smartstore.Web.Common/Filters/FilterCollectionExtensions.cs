using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Web.Filters;

namespace Smartstore
{
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds a filter that is instantiated and invoked only when <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="filterType">Type representing an <see cref="IFilterMetadata"/>.</param>
        [Obsolete("Use AddEndpointFilter<TFilter, TController>().When(x => x...) instead.", false)]
        public static ConditionalFilter AddConditional<TFilter>(this FilterCollection filters, Func<ActionContext, bool> condition, int order = 0)
            where TFilter : IFilterMetadata
        {
            Guard.NotNull(filters);
            Guard.NotNull(condition);

            var filter = new DefaultConditionalFilter<TFilter>(condition) { Order = order };
            filters.Add(filter);
            return filter;
        }

        /// <summary>
        /// Adds a filter that is applied to a specific endpoint when the application starts. Other than <see cref="AddConditional{TFilter}"/>,
        /// which always adds a global filter, this method allows to add a filter that is applied to specific controllers and actions.
        /// </summary>
        /// <param name="filterType">Type representing an <see cref="IFilterMetadata"/>.</param>
        public static EndpointFilterMetadata<TFilter, TController> AddEndpointFilter<TFilter, TController>(this FilterCollection filters, int order = 0)
            where TFilter : IFilterMetadata
            where TController : Controller
        {
            Guard.NotNull(filters);

            var filter = new EndpointFilterMetadata<TFilter, TController>() { Order = order };
            filters.Add(filter);
            return filter;
        }
    }
}
