#nullable enable

namespace Smartstore.Core.Rules
{
    public interface IRuleProviderFactory
    {
        /// <summary>
        /// Gets a provider for a <see cref="RuleScope"/>.
        /// </summary>
        /// <param name="scope"><see cref="RuleScope"/> to get the provider for.</param>
        /// <param name="context">Instance of a context object that is passed to the provider's constructor (optional).</param>
        IRuleProvider GetProvider(RuleScope scope, object? context = null);
    }
}
