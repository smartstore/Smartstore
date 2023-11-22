using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping.Rules
{
    public partial class ShippingRateComputationMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationService _localizationService;

        public ShippingRateComputationMethodRuleOptionsProvider(IProviderManager providerManager, ILocalizationService localizationService)
        {
            _providerManager = providerManager;
            _localizationService = localizationService;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.ShippingRateComputationMethod;

        public Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            RuleOptionsResult result = null;

            if (context.DataSource == KnownRuleOptionDataSourceNames.ShippingRateComputationMethod)
            {
                var options = _providerManager.GetAllProviders<IShippingRateComputationMethod>()
                    .Select(x => x.Metadata)
                    .Select(x => new RuleValueSelectListOption
                    {
                        Value = x.SystemName,
                        Text = GetLocalized(context, x, "FriendlyName") ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                        Hint = x.SystemName
                    })
                    .ToList();

                result = RuleOptionsResult.Create(context, options.OrderBy(x => x.Text));
            }

            return Task.FromResult(result);
        }

        private string GetLocalized(RuleOptionsContext context, ProviderMetadata metadata, string propertyName)
        {
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            var resource = _localizationService.GetResource(resourceName, context.Language.Id, false, string.Empty, true);

            return resource.NullEmpty();
        }
    }
}
