using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class SpentAmountRule : IRule
    {
        private readonly SmartDbContext _db;

        public SpentAmountRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var query = _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .ApplyStatusFilter(new[] { (int)OrderStatus.Complete });

            var spentAmount = await query.SumAsync(x => (decimal?)x.OrderTotal);
            var roundedAmount = decimal.Round(spentAmount ?? decimal.Zero, context.Store.PrimaryStoreCurrency.RoundNumDecimals);

            return expression.Operator.Match(roundedAmount, expression.Value);
        }
    }
}
