using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class OrderCountRule : IRule
    {
        private readonly SmartDbContext _db;

        public OrderCountRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var count = await _db.Orders
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .CountAsync();

            return expression.Operator.Match(count, expression.Value);
        }
    }
}
