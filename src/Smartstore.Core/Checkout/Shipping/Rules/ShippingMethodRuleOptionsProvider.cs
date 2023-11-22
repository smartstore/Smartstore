using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Checkout.Shipping.Rules
{
    public partial class ShippingMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ShippingMethodRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.ShippingMethod;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.ShippingMethod)
            {
                return null;
            }

            var shippingMethods = await _db.ShippingMethods
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var options = shippingMethods.Select(x => new RuleValueSelectListOption
            {
                Value = context.OptionById ? x.Id.ToString() : x.Name,
                Text = context.OptionById ? x.GetLocalized(y => y.Name, context.Language, true, false) : x.Name
            });

            return RuleOptionsResult.Create(context, options);
        }
    }
}
