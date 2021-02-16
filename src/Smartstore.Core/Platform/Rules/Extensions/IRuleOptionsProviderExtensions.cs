using System.Threading.Tasks;

namespace Smartstore.Core.Rules.Rendering
{
    public static partial class IRuleOptionsProviderExtensions
    {
        /// <summary>
        /// Gets options for a rule.
        /// </summary>
        /// <param name="provider">Rule options provider.</param>
        /// <param name="reason">The reason for the request.</param>
        /// <param name="expression">Rule expression</param>
        /// <param name="pageIndex">Page index if provided options are paged.</param>
        /// <param name="pageSize">Page size if provided options are paged.</param>
        /// <param name="searchTerm">Optional search term entered by user in select control.</param>
        /// <returns>Rule options result.</returns>
        public static async Task<RuleOptionsResult> GetOptionsAsync(
            this IRuleOptionsProvider provider,
            RuleOptionsRequestReason reason, 
            IRuleExpression expression,
            int pageIndex, 
            int pageSize, 
            string searchTerm)
        {
            Guard.NotNull(expression, nameof(expression));

            return await provider.GetOptionsAsync(reason, expression.Descriptor, expression.RawValue, pageIndex, pageSize, searchTerm);
        }
    }
}
