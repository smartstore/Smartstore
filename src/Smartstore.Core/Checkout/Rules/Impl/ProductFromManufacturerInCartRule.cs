using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductFromManufacturerInCartRule : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db;

        public ProductFromManufacturerInCartRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var manufacturerIds = Enumerable.Empty<int>();
            var cart = context.ShoppingCart;
            var productIds = cart.Items.Select(x => x.Item.ProductId).ToArray();

            if (productIds.Any())
            {
                // It's unnecessary to check things like ACL, limited-to-stores, published, deleted etc. here
                // because the products are from shopping cart and it cannot contain hidden products.
                manufacturerIds = await _db.ProductManufacturers
                    .Where(x => productIds.Contains(x.ProductId))
                    .Select(x => x.ManufacturerId)
                    .ToListAsync();
            }

            var match = expression.HasListsMatch(manufacturerIds.Distinct());
            return match;
        }
    }
}
