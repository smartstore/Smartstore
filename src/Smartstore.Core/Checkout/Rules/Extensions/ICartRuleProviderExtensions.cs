using System.Runtime.CompilerServices;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    public static partial class ICartRuleProviderExtensions
    {
        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="provider">Cart rule provider.</param>
        /// <param name="expression">Rule expression.</param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<bool> RuleMatchesAsync(this ICartRuleProvider provider, RuleExpression expression)
        {
            Guard.NotNull(provider, nameof(provider));
            Guard.NotNull(expression, nameof(expression));

            return await provider.RuleMatchesAsync(new[] { expression }, LogicalRuleOperator.And);
        }
    }
}
