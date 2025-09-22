using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    /// <summary>
    /// Checks whether at least one product in the shopping cart comes from the specified categories.
    /// </summary>
    internal class ProductFromCategoryInCartRule(SmartDbContext db) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var categoryIds = Enumerable.Empty<int>();
            var productIds = context.ShoppingCart.Items.ToDistinctArray(x => x.Item.ProductId);

            if (productIds.Length > 0)
            {
                // It's unnecessary to check things like ACL, limited-to-stores, published, deleted etc. here
                // because the products are from shopping cart and it cannot contain hidden products.
                categoryIds = await _db.ProductCategories
                    .Where(x => productIds.Contains(x.ProductId))
                    .Select(x => x.CategoryId)
                    .ToListAsync();
            }

            var match = expression.HasListsMatch(categoryIds.Distinct());
            return match;
        }
    }
}
