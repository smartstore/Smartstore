using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class PriceCalculationContextExtensions
    {
        /// <summary>
        /// TODO: (mg) (core) Describe when ready.
        /// </summary>
        public static void ApplyAttributes(this PriceCalculationContext context, IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(cart, nameof(cart));

            foreach (var parent in cart)
            {
                AddCartItem(parent.Item);

                if (parent.Item.Product.ProductType == ProductType.BundledProduct && parent.Item.Product.BundlePerItemPricing)
                {
                    parent.ChildItems.Each(x => AddCartItem(x.Item));
                }
            }

            void AddCartItem(ShoppingCartItem item)
            {
                if (item.AttributeSelection?.AttributesMap?.Any() ?? false)
                {
                    context.Attributes.Add(new AttributePricingItem
                    {
                        ProductId = item.ProductId,
                        BundleItemId = item.BundleItemId,
                        Selection = item.AttributeSelection
                    });
                }
            }
        }
    }
}
