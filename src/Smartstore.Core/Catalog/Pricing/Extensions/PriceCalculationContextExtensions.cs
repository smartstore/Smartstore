using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class PriceCalculationContextExtensions
    {
        /// <summary>
        /// Adds selected product attributes to be taken into account in the price calculation.
        /// For example required for price adjustments of attributes selected by the customer.
        /// </summary>
        /// <param name="context">The target product calculation context.</param>
        /// <param name="selection">The selected product attributes.</param>
        /// <param name="productId">Product identifier.</param>
        /// <param name="bundleItemId">Bundle item identifier if the related product is a bundle item.</param>
        public static void AddSelectedAttributes(this PriceCalculationContext context, ProductVariantAttributeSelection selection, int productId, int? bundleItemId = null)
        {
            Guard.NotNull(context, nameof(context));

            if (selection?.AttributesMap?.Any() ?? false)
            {
                context.SelectedAttributes.Add(new PriceCalculationAttributes(selection, productId)
                {
                    BundleItemId = bundleItemId
                });
            }
        }

        /// <summary>
        /// Adds selected product attributes of a shopping cart item to be taken into account in the price calculation.
        /// For example required for price adjustments of attributes selected by the customer.
        /// Also adds selected attributes of bundle items if <see cref="Product.BundlePerItemPricing"/> is activated.
        /// </summary>
        /// <param name="context">The target product calculation context.</param>
        /// <param name="cartItem">Shopping cart item.</param>
        public static void AddSelectedAttributes(this PriceCalculationContext context, OrganizedShoppingCartItem cartItem)
        {
            Guard.NotNull(context, nameof(context));

            if (cartItem != null)
            {
                var item = cartItem.Item;

                context.AddSelectedAttributes(item);

                if (item.Product.ProductType == ProductType.BundledProduct && item.Product.BundlePerItemPricing)
                {
                    cartItem.ChildItems.Each(x => context.AddSelectedAttributes(x.Item));
                }
            }
        }

        private static void AddSelectedAttributes(this PriceCalculationContext context, ShoppingCartItem item)
        {
            if (item != null)
            {
                context.AddSelectedAttributes(item.AttributeSelection, item.ProductId, item.BundleItemId);
            }
        }
    }
}
