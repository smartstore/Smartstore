using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    /// <summary>
    /// Checks whether all products in the shopping cart are from the specified categories.
    /// </summary>
    internal class AllProductsFromCategoryInCartRule(SmartDbContext db) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var productIds = context.ShoppingCart.Items.ToDistinctArray(x => x.Item.ProductId);
            if (productIds.Length > 0)
            {
                var categoryIds = (await _db.ProductCategories
                    .Where(x => productIds.Contains(x.ProductId))
                    .Select(x => new { x.ProductId, x.CategoryId })
                    .ToListAsync())
                    .ToMultimap(x => x.ProductId, x => x.CategoryId);
                if (categoryIds.Count == 0)
                {
                    return false;
                }

                foreach (var pair in categoryIds)
                {
                    if (!expression.HasListsMatch(pair.Value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
