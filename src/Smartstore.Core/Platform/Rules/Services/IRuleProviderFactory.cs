#nullable enable

namespace Smartstore.Core.Rules
{
    public interface IRuleProviderFactory
    {
        /// <summary>
        /// Gets a provider for a <see cref="RuleScope"/>.
        /// </summary>
        /// <param name="scope"><see cref="RuleScope"/> to get the provider for.</param>
        /// <param name="context">Provider specific instance of a context object that is passed to the provider's constructor.</param>
        IRuleProvider GetProvider(RuleScope scope, object? context = null);
    }


    public static class IRuleProviderFactoryExtensions
    {
        /// <inheritdoc cref="IRuleProviderFactory.GetProvider(RuleScope, object?)" />
        public static T GetProvider<T>(this IRuleProviderFactory factory, RuleScope scope, object? context = null) where T : IRuleProvider
            => (T)Guard.NotNull(factory).GetProvider(scope, context);
    }
}
