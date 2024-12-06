using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CustomerAffiliateRule : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db;

        public CustomerAffiliateRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var customer = context.Customer;
            if (customer != null && !customer.IsSystemAccount && customer.AffiliateId != 0 && expression.HasListMatch(customer.AffiliateId))
            {
                var isValidAffiliate = await _db.Affiliates.AnyAsync(x => x.Id == customer.AffiliateId && !x.Deleted && x.Active);
                return isValidAffiliate;
            }

            return false;
        }
    }
}
