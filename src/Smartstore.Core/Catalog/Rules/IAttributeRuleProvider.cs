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

        Task<IRuleExpressionGroup> CreateExpressionGroupAsync(ProductVariantAttribute attribute, bool includeHidden = false);

        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="context">Attribute rule context.</param>
        /// <param name="logicalOperator">Rule operator.</param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        Task<bool> RuleMatchesAsync(AttributeRuleContext context, LogicalRuleOperator logicalOperator = LogicalRuleOperator.And);
    }
}
