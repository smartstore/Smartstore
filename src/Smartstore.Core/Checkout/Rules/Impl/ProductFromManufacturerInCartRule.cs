using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class ProductFromManufacturerInCartRule : IRule
    {
        private readonly SmartDbContext _db;
        private readonly IShoppingCartService _shoppingCartService;

        public ProductFromManufacturerInCartRule(SmartDbContext db, IShoppingCartService shoppingCartService)
        {
            _db = db;
            _shoppingCartService = shoppingCartService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var manufacturerIds = Enumerable.Empty<int>();
            var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
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
