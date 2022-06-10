using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Common.Rules
{
    public partial class CommonRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public CommonRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
        {
            switch (dataSource)
            {
                case KnownRuleOptionDataSourceNames.Country:
                case KnownRuleOptionDataSourceNames.Currency:
                case KnownRuleOptionDataSourceNames.DeliveryTime:
                    return true;
                default:
                    return false;
            }
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            switch (context.DataSource)
            {
                case KnownRuleOptionDataSourceNames.Country:
                    var countries = await _db.Countries
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    result.AddOptions(context, countries.Select(x => new RuleValueSelectListOption
                    {
                        Value = context.OptionById ? x.Id.ToString() : x.TwoLetterIsoCode,
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                    }));
                    break;

                case KnownRuleOptionDataSourceNames.Currency:
                    var currencies = await _db.Currencies
                        .AsNoTracking()
                        .ApplyStandardFilter(true)
                        .ToListAsync();

                    result.AddOptions(context, currencies.Select(x => new RuleValueSelectListOption
                    {
                        Value = context.OptionById ? x.Id.ToString() : x.CurrencyCode,
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                    }));
                    break;

                case KnownRuleOptionDataSourceNames.DeliveryTime:
                    var deliveryTimes = await _db.DeliveryTimes
                        .AsNoTracking()
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    result.AddOptions(context, deliveryTimes.Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                    }));
                    break;

                default:
                    return null;
            }

            return result;
        }
    }
}
