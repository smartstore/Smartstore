namespace Smartstore.Core.Rules.Rendering
{
    public enum RuleOptionsRequestReason
    {
        /// <summary>
        /// Get options for select list.
        /// </summary>
        SelectListOptions = 0,

        /// <summary>
        /// Get display names of selected options.
        /// </summary>
        SelectedDisplayNames
    }


    public class RuleOptionsResult
    {
        public static readonly RuleOptionsResult Empty = new();

        /// <summary>
        /// Creates a <see cref="RuleOptionsResult"/> instance with given <paramref name="options"/>.
        /// </summary>
        public static RuleOptionsResult Create(
            RuleOptionsContext context,
            IEnumerable<RuleValueSelectListOption> options,
            bool isPaged = false,
            bool hasMoreData = false)
        {
            var result = new RuleOptionsResult
            {
                IsPaged = isPaged,
                HasMoreData = hasMoreData
            };

            result.AddOptions(context, options);
            return result;
        }

        /// <summary>
        /// Select list options or display names of selected values, depending on <see cref="RuleOptionsRequestReason"/>.
        /// </summary>
        public IList<RuleValueSelectListOption> Options { get; init; } = new List<RuleValueSelectListOption>();

        /// <summary>
        /// Indicates whether the provided data is paged.
        /// </summary>
        public bool IsPaged { get; set; }

        /// <summary>
        /// Indicates whether further data is available.
        /// </summary>
        public bool HasMoreData { get; set; }

        /// <summary>
        /// Adds rule options to this result instance.
        /// </summary>
        /// <param name="context">Rule options context.</param>
        /// <param name="options">Options to add.</param>
        public void AddOptions(RuleOptionsContext context, IEnumerable<RuleValueSelectListOption> options)
        {
            Guard.NotNull(context);

            if (options != null)
            {
                if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
                {
                    // Get display names of selected options.
                    if (context.Value.HasValue())
                    {
                        var selectedValues = context.Value.SplitSafe(',');
                        Options.AddRange(options.Where(x => selectedValues.Contains(x.Value)));
                    }
                }
                else
                {
                    // Get select list options.
                    if (!IsPaged && context.SearchTerm.HasValue() && options.Any())
                    {
                        // Apply the search term if the options are not paged.
                        Options.AddRange(options.Where(x => (x.Text?.IndexOf(context.SearchTerm, 0, StringComparison.CurrentCultureIgnoreCase) ?? -1) != -1));
                    }
                    else
                    {
                        Options.AddRange(options);
                    }
                }
            }
        }
    }
}
