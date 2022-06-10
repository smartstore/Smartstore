namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Interface to provide select list options (remote only) for rules. <seealso cref="RemoteRuleValueSelectList"/>.
    /// </summary>
    public partial interface IRuleOptionsProvider
    {
        /// <summary>
        /// Gets the ordinal number of the provider.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Indicates whether this provider can provide select list options for a rule expression.
        /// </summary>
        /// <param name="dataSource">Name of the data source.</param>
        /// <returns><c>true</c> can provide options otherwise <c>false</c>.</returns>
        bool Matches(string dataSource);

        /// <summary>
        /// Gets options for a rule.
        /// </summary>
        /// <param name="context">Rule options context.</param>
        /// <returns>Rule options result.</returns>
        Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context);
    }
}
