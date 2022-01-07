using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class SpentAmountRule : IRule
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;

        public SpentAmountRule(SmartDbContext db, ICurrencyService currencyService)
        {
            _db = db;
            _currencyService = currencyService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var query = _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .ApplyStatusFilter(new[] { (int)OrderStatus.Complete });

            var spentAmount = await query.SumAsync(x => (decimal?)x.OrderTotal);
            var roundedAmount = decimal.Round(spentAmount ?? decimal.Zero, _currencyService.PrimaryCurrency.RoundNumDecimals);

            return expression.Operator.Match(roundedAmount, expression.Value);
        }
    }
}
