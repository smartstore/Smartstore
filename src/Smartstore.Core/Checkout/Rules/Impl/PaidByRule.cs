using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class PaidByRule : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db;

        public PaidByRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var query = _db.Orders
                .AsNoTracking()
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id);

            if (expression.Operator == RuleOperator.In || expression.Operator == RuleOperator.NotIn)
            {
                // Find match using LINQ to Entities.
                var paymentMethods = expression.Value as List<string>;
                if (!(paymentMethods?.Any() ?? false))
                {
                    return true;
                }

                if (expression.Operator == RuleOperator.In)
                {
                    return await query.Where(o => paymentMethods.Contains(o.PaymentMethodSystemName)).AnyAsync();
                }

                return await query.Where(o => !paymentMethods.Contains(o.PaymentMethodSystemName)).AnyAsync();
            }
            else
            {
                // Find match using LINQ to Objects.
                var paymentMethods = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                var pager = query.ToFastPager(4000);

                while ((await pager.ReadNextPageAsync(x => new { x.Id, x.PaymentMethodSystemName }, x => x.Id)).Out(out var orders))
                {
                    paymentMethods.AddRange(orders.Select(x => x.PaymentMethodSystemName));
                }

                var match = expression.HasListsMatch(paymentMethods, StringComparer.InvariantCultureIgnoreCase);
                return match;
            }
        }
    }
}
