using Smartstore.Core.Localization;

namespace Smartstore.Core.Rules.Rendering
{
    /// <summary>
    /// Helper to get rule options.
    /// </summary>
    public class RuleOptionsContext
    {
        public RuleOptionsContext(RuleOptionsRequestReason reason, IRuleExpression expression)
            : this(reason, expression?.Descriptor)
        {
            Guard.NotNull(expression, nameof(expression));

            Value = expression.RawValue;
        }

        public RuleOptionsContext(RuleOptionsRequestReason reason, RuleDescriptor descriptor)
        {
            Guard.NotNull(descriptor, nameof(descriptor));
            Guard.NotNull(reason, nameof(reason));

            Descriptor = descriptor;
            Reason = reason;

            if (descriptor.SelectList is RemoteRuleValueSelectList list)
            {
                DataSource = list.DataSource;
            }

            Guard.NotEmpty(DataSource, nameof(DataSource));
        }

        /// <summary>
        /// Rule descriptor.
        /// </summary>
        public RuleDescriptor Descriptor { get; init; }

        /// <summary>
        /// Rule options request.
        /// </summary>
        public RuleOptionsRequestReason Reason { get; init; }

        /// <summary>
        /// Data source name of the remote list.
        /// </summary>
        public string DataSource { get; init; }

        /// <summary>
        /// Language.
        /// </summary>
        public Language Language { get; init; }

        /// <summary>
        /// Rule expression value.
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// Page index if provided options are paged.
        /// </summary>
        public int PageIndex { get; init; }

        /// <summary>
        /// Page size if provided options are paged.
        /// </summary>
        public int PageSize { get; init; } = 100;

        /// <summary>
        /// Optional search term to reduce the options result.
        /// </summary>
        public string SearchTerm { get; init; }

        /// <summary>
        /// Gets a value indicating whether to return the entity ID as option value.
        /// </summary>
        public bool OptionById => Descriptor.RuleType == RuleType.Int || Descriptor.RuleType == RuleType.IntArray;
    }
}
