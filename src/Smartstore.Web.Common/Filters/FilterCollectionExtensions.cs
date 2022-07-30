using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Web.Filters;

namespace Smartstore
{
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds a filter that is instantiated and invoked only when <paramref name="condition"/> is <c>true</c>.
        /// </summary>
        /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
        public static IFilterMetadata AddConditional<TFilterType>(this FilterCollection filters, Func<ActionContext, bool> condition, int order = 0)
            where TFilterType : IFilterMetadata
        {
            Guard.NotNull(filters, nameof(filters));
            Guard.NotNull(condition, nameof(condition));

            var filter = new DefaultConditionalFilter<TFilterType>(condition) { Order = order };
            filters.Add(filter);
            return filter;
        }
    }
}
