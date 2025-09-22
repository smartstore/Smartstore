using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    /// <summary>
    /// Checks whether at least one product in the shopping cart comes from the specified categories including subcategories.
    /// </summary>
    internal class ProductInCategoryTreeCartRule : IRule<CartRuleContext>
    {
        private readonly SmartDbContext _db;
        private readonly ICategoryService _categoryService;

        public ProductInCategoryTreeCartRule(SmartDbContext db, ICategoryService categoryService)
        {
            _db = db;
            _categoryService = categoryService;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            if (expression.Value is not List<int> rightValues || rightValues.Count == 0)
            {
                return true;
            }

            var productIds = context.ShoppingCart.Items.ToDistinctArray(x => x.Item.ProductId);
            if (productIds.Length == 0)
            {
                return expression.HasListsMatch(Enumerable.Empty<int>());
            }

            var tree = await _categoryService.GetCategoryTreeAsync(0, false, context.Store.Id);
            var rightCategoryIds = rightValues
                .SelectMany(id =>
                {
                    var node = tree.SelectNodeById(id);
                    return node != null ? node.Flatten(true).Select(y => y.Id) : [];
                })
                .Distinct()
                .ToList();

            var leftCategoryIds = await _db.ProductCategories
                .Where(x => productIds.Contains(x.ProductId))
                .Select(x => x.CategoryId)
                .ToListAsync();

            return expression.HasListsMatch(leftCategoryIds.Distinct(), rightCategoryIds);
        }
    }
}
