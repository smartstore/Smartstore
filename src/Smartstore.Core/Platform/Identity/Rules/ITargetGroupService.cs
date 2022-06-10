using Smartstore.Collections;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Identity.Rules
{
    /// <summary>
    /// Target group service interface.
    /// </summary>
    public partial interface ITargetGroupService : IRuleProvider
    {
        /// <summary>
        /// Creates a filter expression group.
        /// </summary>
        /// <param name="ruleSetId">Rule set identifier.</param>
        /// <returns>Filter expression group.</returns>
        Task<FilterExpressionGroup> CreateExpressionGroupAsync(int ruleSetId);

        /// <summary>
        /// Processes target group filters.
        /// </summary>
        /// <param name="ruleSetIds">Ruleset identifiers.</param>
        /// <param name="logicalOperator">Rule operator.</param>
        /// <param name="pageIndex">Page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>List of customers.</returns>
        Task<IPagedList<Customer>> ProcessFilterAsync(
            int[] ruleSetIds,
            LogicalRuleOperator logicalOperator,
            int pageIndex = 0,
            int pageSize = int.MaxValue);

        /// <summary>
        /// Processes target group filters.
        /// </summary>
        /// <param name="filters">Filter expressions.</param>
        /// <param name="logicalOperator">Rule operator.</param>
        /// <param name="pageIndex">Page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>List of customers.</returns>
        IPagedList<Customer> ProcessFilter(
            FilterExpression[] filters,
            LogicalRuleOperator logicalOperator,
            int pageIndex = 0,
            int pageSize = int.MaxValue);
    }
}
