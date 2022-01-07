namespace Smartstore.Core.Rules
{
    /// <summary>
    /// Rule visitor interface.
    /// </summary>
    public interface IRuleVisitor
    {
        /// <summary>
        /// Gets the rule scope.
        /// </summary>
        RuleScope Scope { get; }

        /// <summary>
        /// Visits a rule.
        /// </summary>
        /// <param name="rule">Rule.</param>
        /// <returns>Rule expression.</returns>
        Task<IRuleExpression> VisitRuleAsync(RuleEntity rule);

        /// <summary>
        /// Visits a ruleset.
        /// </summary>
        /// <param name="ruleSet">Ruleset.</param>
        /// <returns>Rule expression group.</returns>
        IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet);
    }
}