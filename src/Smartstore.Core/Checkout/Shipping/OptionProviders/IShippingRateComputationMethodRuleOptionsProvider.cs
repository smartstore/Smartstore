using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Checkout.Shipping
{
    public partial class IShippingRateComputationMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly IProviderManager _providerManager;
        private readonly ILocalizationService _localizationService;

        public IShippingRateComputationMethodRuleOptionsProvider(IProviderManager providerManager, ILocalizationService localizationService)
        {
            _providerManager = providerManager;
            _localizationService = localizationService;
        }

        public int Ordinal => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.ShippingRateComputationMethod;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.ShippingRateComputationMethod)
            {
                var options = await _providerManager.GetAllProviders<IShippingRateComputationMethod>()
                    .Select(x => x.Metadata)
                    .SelectAsync(async x => new RuleValueSelectListOption
                    {
                        Value = x.SystemName,
                        Text = await GetLocalized(context, x, "FriendlyName") ?? x.FriendlyName.NullEmpty() ?? x.SystemName,
                        Hint = x.SystemName
                    })
                    .ToListAsync();

                result.AddOptions(context, options.OrderBy(x => x.Text));
            }
            else
            {
                return null;
            }

            return result;
        }

        private async Task<string> GetLocalized(RuleOptionsContext context, ProviderMetadata metadata, string propertyName)
        {
            var resourceName = metadata.ResourceKeyPattern.FormatInvariant(metadata.SystemName, propertyName);
            var resource = await _localizationService.GetResourceAsync(resourceName, context.Language.Id, false, string.Empty, true);

            return resource.NullEmpty();
        }
    }
}
