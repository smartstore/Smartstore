#nullable enable

namespace Smartstore.Core.Rules
{
    /// <summary>
    /// Rule interface.
    /// </summary>
    public interface IRule<in TContext> where TContext : class
    {
        /// <summary>
        /// Checks whether a rule is met.
        /// </summary>
        /// <param name="context">Rule context.</param>
        /// <param name="expression">Rule expression.</param>
        /// <returns><c>true</c> the rule is met, otherwise <c>false</c>.</returns>
        Task<bool> MatchAsync(TContext context, RuleExpression expression);
    }
}
