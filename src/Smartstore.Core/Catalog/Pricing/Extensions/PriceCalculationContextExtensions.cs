using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class PriceCalculationContextExtensions
    {
        // TODO: (mg) (core) Describe pricing pipeline when ready.

        public static void AddAttributes(this PriceCalculationContext context, ProductVariantAttributeSelection selection, int productId, int? bundleItemId = null)
        {
            Guard.NotNull(context, nameof(context));

            if (selection?.AttributesMap?.Any() ?? false)
            {
                context.Attributes.Add(new PriceCalculationAttributes(selection, productId)
                {
                    BundleItemId = bundleItemId
                });
            }
        }

        public static void AddAttributes(this PriceCalculationContext context, ShoppingCartItem item)
        {
            Guard.NotNull(context, nameof(context));

            if (item != null)
            {
                context.AddAttributes(item.AttributeSelection, item.ProductId, item.BundleItemId);
            }
        }

        public static void AddAttributes(this PriceCalculationContext context, IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(context, nameof(context));

            var item = cart?.FirstOrDefault(x => x.Item.ProductId == context.Product.Id);
            if (item?.Item != null)
            {
                context.AddAttributes(item.Item);

                if (item.Item.Product.ProductType == ProductType.BundledProduct && item.Item.Product.BundlePerItemPricing)
                {
                    item.ChildItems.Each(x => context.AddAttributes(x.Item));
                }
            }
        }
    }
}
