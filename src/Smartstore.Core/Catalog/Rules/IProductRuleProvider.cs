using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    /// <summary>
    /// Interface for a product rule provider.
    /// </summary>
    public partial interface IProductRuleProvider : IRuleProvider
    {
        /// <summary>
        /// Creates a rule expression group.
        /// </summary>
        /// <param name="ruleSetId">Rule set identifier.</param>
        /// <returns>Expression group.</returns>
        Task<SearchFilterExpressionGroup> CreateExpressionGroupAsync(int ruleSetId);

        /// <summary>
        /// Searches for products that match the filter expressions.
        /// </summary>
        /// <param name="filters">Filter expressions.</param>
        /// <param name="pageIndex">Page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>Catalog search result.</returns>
        Task<CatalogSearchResult> SearchAsync(SearchFilterExpression[] filters, int pageIndex = 0, int pageSize = int.MaxValue);
    }
}
