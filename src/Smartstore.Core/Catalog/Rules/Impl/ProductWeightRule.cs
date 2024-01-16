using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules.Impl
{
    internal class ProductWeightRule : IRule<AttributeRuleContext>
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly IRoundingHelper _roundingHelper;

        public ProductWeightRule(
            SmartDbContext db,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper)
        {
            _db = db;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
        }

        public async Task<bool> MatchAsync(AttributeRuleContext context, RuleExpression expression)
        {
            var weight = context.Product.Weight;

            if (context.SelectedValueIds.Length > 0)
            {
                weight += (await _db.ProductVariantAttributeValues
                    .Where(x => context.SelectedValueIds.Contains(x.Id))
                    .SumAsync(x => (decimal?)x.WeightAdjustment)) ?? 0m;
            }

            weight = _roundingHelper.Round(weight, _currencyService.PrimaryCurrency);

            var match = expression.Operator.Match(weight, expression.Value);
            return match;
        }
    }
}
