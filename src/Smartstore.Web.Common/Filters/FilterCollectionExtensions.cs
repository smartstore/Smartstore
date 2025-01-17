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
            Guard.NotNull(filters);
            Guard.NotNull(condition);

            var filter = new DefaultConditionalFilter<TFilterType>(condition) { Order = order };
            filters.Add(filter);
            return filter;
        }

        /// <summary>
        /// TODO: Add summary
        /// </summary>
        /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
        public static IFilterMetadata AddActionFilter<TFilterType, TController>(this FilterCollection filters, Expression<Func<TController, Task<IActionResult>>> actionSelector, int order = 0)
            where TFilterType : IFilterMetadata
            where TController : Controller
        {
            Guard.NotNull(filters);
            Guard.NotNull(actionSelector);

            return filters.AddConditional<TFilterType>(context => context.ControllerIs(actionSelector), order);
        }

        /// <summary>
        /// TODO: Add summary
        /// </summary>
        /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
        public static IFilterMetadata AddActionFilter<TFilterType, TController>(this FilterCollection filters, Expression<Func<TController, IActionResult>> actionSelector, int order = 0)
            where TFilterType : IFilterMetadata
            where TController : Controller
        {
            Guard.NotNull(filters);
            Guard.NotNull(actionSelector);

            return filters.AddConditional<TFilterType>(context => context.ControllerIs(actionSelector), order);
        }

        /// <summary>
        /// TODO: Add summary
        /// </summary>
        /// <param name="filterType">Type representing a <see cref="IFilterMetadata"/>.</param>
        public static IFilterMetadata AddControllerFilter<TFilterType, TController>(this FilterCollection filters, int order = 0)
            where TFilterType : IFilterMetadata
            where TController : Controller
        {
            Guard.NotNull(filters);

            return filters.AddConditional<TFilterType>(context => context.ControllerIs<TController>(), order);
        }
    }
}
