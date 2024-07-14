using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class OrderCountRule(SmartDbContext db) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var count = await _db.Orders
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .CountAsync();

            return expression.Operator.Match(count, expression.Value);
        }
    }
}
