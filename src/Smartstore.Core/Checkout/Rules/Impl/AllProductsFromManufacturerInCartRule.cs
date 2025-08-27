using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    /// <summary>
    /// Checks whether all products in the shopping cart originate from the specified manufacturers.
    /// </summary>
    internal class AllProductsFromManufacturerInCartRule(SmartDbContext db) : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var productIds = context.ShoppingCart.Items.ToDistinctArray(x => x.Item.ProductId);
            if (productIds.Length > 0)
            {
                var manufacturerIds = (await _db.ProductManufacturers
                    .Where(x => productIds.Contains(x.ProductId))
                    .Select(x => new { x.ProductId, x.ManufacturerId })
                    .ToListAsync())
                    .ToMultimap(x => x.ProductId, x => x.ManufacturerId);

                if (manufacturerIds.Count == 0)
                {
                    return false;
                }

                foreach (var pair in manufacturerIds)
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
