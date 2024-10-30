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
        /// Checks whether a product meets rules.
        /// </summary>
        /// <param name="productId">Identifier of the product to be checked.</param>
        /// <param name="ruleSets">Rules to be checked.</param>
        /// <param name="logicalOperator">The rule operator if <paramref name="ruleSets"/> contains more than one <see cref="RuleSetEntity"/>.</param>
        /// <returns><c>true</c> the rules are met, otherwise <c>false</c>.</returns>
        Task<bool> MatchesAsync(int productId, IEnumerable<RuleSetEntity> ruleSets, LogicalRuleOperator logicalOperator = LogicalRuleOperator.Or);

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
