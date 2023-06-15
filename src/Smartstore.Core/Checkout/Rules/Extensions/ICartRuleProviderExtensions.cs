#nullable enable

using System.Runtime.CompilerServices;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules
{
    public static partial class ICartRuleProviderExtensions
    {
        /// <inheritdoc cref="ICartRuleProvider.RuleMatchesAsync(int[], LogicalRuleOperator, Action{CartRuleContext}?) "/>
        /// <param name="expression">Rule expression.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<bool> RuleMatchesAsync(
            this ICartRuleProvider provider, 
            RuleExpression expression,
            Action<CartRuleContext>? contextAction = null)
        {
            Guard.NotNull(provider);
            Guard.NotNull(expression);

            return await provider.RuleMatchesAsync(new[] { expression }, LogicalRuleOperator.And, contextAction);
        }
    }
}
