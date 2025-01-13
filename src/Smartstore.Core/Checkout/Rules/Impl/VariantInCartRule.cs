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
