#nullable enable

using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    /// <summary>
    /// Represents a product attribute rule provider (conditional attributes).
    /// </summary>
    public partial interface IAttributeRuleProvider : IRuleProvider
    {
        /// <summary>
        /// Gets the rule processor.
        /// </summary>
        /// <param name="expression">Rule expression.</param>
        /// <returns>Rule processor.</returns>
        IRule<AttributeRuleContext> GetProcessor(RuleExpression expression);

        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="attribute">The attribute with the rules to be checked.</param>
        /// <param name="logicalOperator">Rule operator.</param>
        /// <param name="contextAction">
        /// An optional action delegate to change the <see cref="AttributeRuleContext"/> 
        /// instance that is passed to the rules.
        /// </param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        Task<bool> RuleMatchesAsync(
            ProductVariantAttribute attribute,
            LogicalRuleOperator logicalOperator,
            Action<AttributeRuleContext>? contextAction = null);

        /// <inheritdoc cref="RuleMatchesAsync(ProductVariantAttribute, LogicalRuleOperator, Action{AttributeRuleContext}?) "/>
        /// <param name="expressions">Rule expressions.</param>
        /// <param name="logicalOperator"></param>
        /// <param name="contextAction"></param>
        /// <returns></returns>
        Task<bool> RuleMatchesAsync(
            RuleExpression[] expressions,
            LogicalRuleOperator logicalOperator,
            Action<AttributeRuleContext>? contextAction = null);
    }
}
