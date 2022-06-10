using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    /// <summary>
    /// Rule interface.
    /// </summary>
    public interface IRule
    {
        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="context">Cart rule context.</param>
        /// <param name="expression">Rule expression.</param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression);
    }
}
