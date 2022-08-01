
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Payment.Rules
{
    public partial class PaymentMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationService _localizationService;

        public PaymentMethodRuleOptionsProvider(IProviderManager providerManager, ILocalizationService localizationService)
        {
            _providerManager = providerManager;
            _localizationService = localizationService;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.PaymentMethod;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource == KnownRuleOptionDataSourceNames.PaymentMethod)
            {
                var result = new RuleOptionsResult();

                var options = await _providerManager.GetAllProviders<IPaymentMethod>()
                    .Select(x => x.Metadata)
                    .SelectAwait(async x => new RuleValueSelectListOption
                    {
                        Value = x.SystemName,
                        Text = await GetLocalized(context, x, "FriendlyName") ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                        Hint = x.SystemName
                    })
                    .ToListAsync();

                result.AddOptions(context, options.OrderBy(x => x.Text).ToList());

                return result;
            }

            return null;
        }

        private async Task<string> GetLocalized(RuleOptionsContext context, ProviderMetadata metadata, string propertyName)
        {
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            var resource = await _localizationService.GetResourceAsync(resourceName, context.Language.Id, false, string.Empty, true);

            return resource.NullEmpty();
        }
    }
}
