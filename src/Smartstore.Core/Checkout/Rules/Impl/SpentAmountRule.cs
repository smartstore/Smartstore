using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class SpentAmountRule(
        SmartDbContext db,
        ICurrencyService currencyService,
        IRoundingHelper roundingHelper) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;
        private readonly ICurrencyService _currencyService = currencyService;
        private readonly IRoundingHelper _roundingHelper = roundingHelper;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var query = _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .ApplyStatusFilter(new[] { (int)OrderStatus.Complete });

            var spentAmount = await query.SumAsync(x => (decimal?)x.OrderTotal);
            var roundedAmount = _roundingHelper.Round(spentAmount ?? 0m, _currencyService.PrimaryCurrency);

            return expression.Operator.Match(roundedAmount, expression.Value);
        }
    }
}
