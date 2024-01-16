#nullable enable

using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    /// <summary>
    /// Cart rule provider.
    /// </summary>
    public partial interface ICartRuleProvider : IRuleProvider
    {
        /// <summary>
        /// Gets the rule processor.
        /// </summary>
        /// <param name="expression">Rule expression.</param>
        /// <returns>Rule processor.</returns>
        IRule<CartRuleContext> GetProcessor(RuleExpression expression);

        /// <summary>
        /// Creates a rule expression group.
        /// </summary>
        /// <param name="ruleSetId">Rule set identifier.</param>
        /// <returns>Expression group.</returns>
        Task<RuleExpressionGroup?> CreateExpressionGroupAsync(int ruleSetId);

        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="ruleSetIds">Rule set identifiers.</param>
        /// <param name="logicalOperator">Rule operator.</param>
        /// <param name="contextAction">
        /// An optional action delegate to change the <see cref="CartRuleContext"/> 
        /// instance that is passed to the rules.
        /// </param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        Task<bool> RuleMatchesAsync(
            int[] ruleSetIds, 
            LogicalRuleOperator logicalOperator, 
            Action<CartRuleContext>? contextAction = null);

        /// <inheritdoc cref="RuleMatchesAsync(int[], LogicalRuleOperator, Action{CartRuleContext}?) "/>
        /// <param name="entity">Rule container.</param>
        Task<bool> RuleMatchesAsync(
            IRulesContainer entity, 
            LogicalRuleOperator logicalOperator = LogicalRuleOperator.Or, 
            Action<CartRuleContext>? contextAction = null);

        /// <inheritdoc cref="RuleMatchesAsync(int[], LogicalRuleOperator, Action{CartRuleContext}?) "/>
        /// <param name="expressions">Rule expressions.</param>
        Task<bool> RuleMatchesAsync(
            RuleExpression[] expressions, 
            LogicalRuleOperator logicalOperator, 
            Action<CartRuleContext>? contextAction = null);
    }
}
