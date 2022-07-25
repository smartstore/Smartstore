using Newtonsoft.Json.Linq;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class CartItemFromCategoryQuantityRule : IRule
    {
        private readonly SmartDbContext _db;
        private readonly IShoppingCartService _shoppingCartService;

        public CartItemFromCategoryQuantityRule(SmartDbContext db, IShoppingCartService shoppingCartService)
        {
            _db = db;
            _shoppingCartService = shoppingCartService;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            int categoryId = 0;
            int? minQuantity = null;
            int? maxQuantity = null;

            try
            {
                var rawValue = expression.Value as string;
                if (rawValue.HasValue())
                {
                    dynamic json = JObject.Parse(rawValue);
                    categoryId = ((string)json.EntityId).ToInt();

                    var str = (string)json.MinQuantity;
                    if (str.HasValue())
                    {
                        minQuantity = str.ToInt();
                    }

                    str = (string)json.MaxQuantity;
                    if (str.HasValue())
                    {
                        maxQuantity = str.ToInt();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            if (categoryId != 0)
            {
                var productsIds = await _db.ProductCategories
                    .Where(x => x.CategoryId == categoryId)
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToArrayAsync();

                if (productsIds.Length > 0)
                {
                    var cart = await _shoppingCartService.GetCartAsync(context.Customer, ShoppingCartType.ShoppingCart, context.Store.Id);
                    var items = cart.Items.Where(x => productsIds.Contains(x.Item.ProductId));
                    if (items.Any())
                    {
                        var quantity = items.Sum(x => x.Item.Quantity);
                        if (quantity > 0)
                        {
                            if (minQuantity.HasValue && maxQuantity.HasValue)
                            {
                                if (minQuantity == maxQuantity)
                                {
                                    return quantity == minQuantity;
                                }
                                else
                                {
                                    return quantity >= minQuantity && quantity <= maxQuantity;
                                }
                            }
                            else if (minQuantity.HasValue)
                            {
                                return quantity >= minQuantity;
                            }
                            else if (maxQuantity.HasValue)
                            {
                                return quantity <= maxQuantity;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
