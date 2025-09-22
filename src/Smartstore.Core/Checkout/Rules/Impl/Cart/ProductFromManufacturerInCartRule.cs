using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    /// <summary>
    /// Checks whether at least one product in the shopping cart originates from the specified manufacturers.
    /// </summary>
    internal class ProductFromManufacturerInCartRule(SmartDbContext db) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var manufacturerIds = Enumerable.Empty<int>();
            var productIds = context.ShoppingCart.Items.ToDistinctArray(x => x.Item.ProductId);

            if (productIds.Length > 0)
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
