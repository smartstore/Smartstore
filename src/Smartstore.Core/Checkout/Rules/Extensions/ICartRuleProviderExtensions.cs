#nullable enable

using System.Runtime.CompilerServices;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules;

public static partial class ICartRuleProviderExtensions
{
    /// <inheritdoc cref="ICartRuleProvider.RuleMatchesAsync(int[], LogicalRuleOperator, Action{CartRuleContext}?) "/>
    /// <param name="expression">Rule expression.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<bool> RuleMatchesAsync(this ICartRuleProvider provider,
        RuleExpression expression,
        Action<CartRuleContext>? contextAction = null)
    {
        Guard.NotNull(provider);
        Guard.NotNull(expression);

        return await provider.RuleMatchesAsync([expression], LogicalRuleOperator.And, contextAction);
    }

    /// <summary>
    /// Returns entities without active cart rule sets or for which at least one assigned rule set matches.
    /// The operator defined by each rule set controls how its own rules and groups are combined.
    /// Shared rule sets are evaluated only once per invocation.
    /// </summary>
    /// <typeparam name="T">Rule container type.</typeparam>
    /// <param name="provider">Cart rule provider.</param>
    /// <param name="entities">Entities to filter.</param>
    /// <param name="contextAction">Optional action to initialize the cart rule context.</param>
    /// <returns>The entities permitted by their assigned cart rule sets.</returns>
    public static async Task<List<T>> FilterByCartRulesAsync<T>(this ICartRuleProvider provider,
        IEnumerable<T> entities,
        Action<CartRuleContext>? contextAction = null)
        where T : IRulesContainer
    {
        Guard.NotNull(provider);
        Guard.NotNull(entities);

        // A rule set can be assigned to multiple entities. Reuse its result within this operation.
        var ruleSetMatches = new Dictionary<int, bool>();

        return await entities
            .WhereAwait(async x =>
            {
                var ruleSets = x.RuleSets
                    .Where(x => x.Scope == RuleScope.Cart && x.IsActive)
                    .ToArray();

                // Entities without active cart rule sets are unrestricted.
                if (ruleSets.Length == 0)
                {
                    return true;
                }

                // Multiple assigned rule sets use OR semantics.
                foreach (var ruleSet in ruleSets)
                {
                    if (!ruleSetMatches.TryGetValue(ruleSet.Id, out var matches))
                    {
                        var expression = await provider.CreateExpressionGroupAsync(ruleSet.Id);
                        matches = expression == null || await provider.RuleMatchesAsync([expression], LogicalRuleOperator.And, contextAction);
                        ruleSetMatches[ruleSet.Id] = matches;
                    }

                    if (matches)
                    {
                        return true;
                    }
                }

                return false;
            })
            .ToListAsync();
    }
}