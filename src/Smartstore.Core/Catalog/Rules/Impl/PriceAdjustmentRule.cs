using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules.Impl
{
    internal class PriceAdjustmentRule(
        SmartDbContext db,
        ICurrencyService currencyService,
        IRoundingHelper roundingHelper) : IRule<AttributeRuleContext>
    {
        private readonly SmartDbContext _db = db;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly IRoundingHelper _roundingHelper = roundingHelper;

        public async Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            decimal roundedPriceAdjustment = 0;

            if (context.SelectedValueIds.Length > 0)
            {
                var priceAdjustments = await _db.ProductVariantAttributeValues
                    .Where(x => context.SelectedValueIds.Contains(x.Id))
                    .SumAsync(x => (decimal?)x.PriceAdjustment);

                roundedPriceAdjustment = _roundingHelper.Round(priceAdjustments ?? 0m, _currencyService.PrimaryCurrency);
            }

            var match = expression.Operator.Match(roundedPriceAdjustment, expression.Value);
            return match;
        }
    }
}
