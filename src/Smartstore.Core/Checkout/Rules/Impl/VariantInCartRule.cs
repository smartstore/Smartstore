using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    internal class VariantInCartRule(IProductAttributeMaterializer productAttributeMaterializer) : IRule<CartRuleContext>
    {
        private readonly IProductAttributeMaterializer _productAttributeMaterializer = productAttributeMaterializer;

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            if (!context.ShoppingCart.HasItems)
            {
                return false;
            }

            var skus = new List<string>();

            // INFO: We have to merge and check the items individually because a product with different variants can appear several times
            // in the cart and a product would overwrite those of the previous one.
            foreach (var item in context.ShoppingCart.Items.Select(x => x.Item))
            {
                if (item.AttributeSelection.HasAttributes)
                {
                    // TODO: (mg) Do you really want to merge here? Can't you just pick the SKU from item or selection and add it to the skus list?
                    // RE: No because it has not been merged anywhere before. Otherwise item.Product.Sku would contain the SKU of the attribute combination,
                    // but it does not. If you want to take it from the cart, you would always have to merge in GetCartAsync, even if the SKU is not needed.
                    await _productAttributeMaterializer.MergeWithCombinationAsync(item.Product, item.AttributeSelection, null);
                    if (item.Product.Sku.HasValue())
                    {
                        skus.Add(item.Product.Sku);
                    }
                }
            }

            var match = expression.HasListsMatch(skus, StringComparer.InvariantCultureIgnoreCase);
            return match;
        }
    }
}
