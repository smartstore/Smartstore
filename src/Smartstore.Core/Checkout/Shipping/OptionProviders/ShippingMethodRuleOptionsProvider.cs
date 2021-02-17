using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Checkout.Shipping
{
    public partial class ShippingMethodRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ShippingMethodRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Ordinal => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.ShippingMethod;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.ShippingMethod)
            {
                var shippingMethods = await _db.ShippingMethods
                    .AsNoTracking()
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync();

                result.AddOptions(context, shippingMethods.Select(x => new RuleValueSelectListOption
                {
                    Value = context.OptionById ? x.Id.ToString() : x.Name,
                    Text = context.OptionById ? x.GetLocalized(y => y.Name, context.Language, true, false) : x.Name
                }));                    
            }
            else
            {
                return null;
            }

            return result;
        }
    }
}
