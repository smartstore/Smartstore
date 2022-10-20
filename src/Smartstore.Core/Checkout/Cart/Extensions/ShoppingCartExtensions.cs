using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Cart
{
    public static partial class ShoppingCartExtensions
    {
        /// <summary>
        /// Finds a cart item in a shopping cart.
        /// </summary>
        /// <remarks>Products with the same identifier need to have matching attribute selections as well.</remarks>
        /// <param name="cart">Shopping cart to search.</param>
        /// <param name="shoppingCartType">Shopping cart type to search.</param>
        /// <param name="product">Product to search for.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <param name="customerEnteredPrice">Customers entered price needs to match (if enabled by product).</param>
        /// <returns>Matching <see cref="OrganizedShoppingCartItem"/> or <c>null</c> if none was found.</returns>
        public static OrganizedShoppingCartItem FindItemInCart(
            this ShoppingCart cart,
            ShoppingCartType shoppingCartType,
            Product product,
            ProductVariantAttributeSelection selection = null,
            Money? customerEnteredPrice = null)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(product, nameof(product));

            // Return on product bundle with individual item pricing. It is too complex to compare.
            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                return null;
            }

            var priceDigits = customerEnteredPrice?.DecimalDigits ?? 2;

            // Filter items of matching cart type, product ID and product type.
            var filteredCart = cart.Items.Where(x => x.Item.ShoppingCartType == shoppingCartType &&
                x.Item.ParentItemId == null &&
                x.Item.Product.ProductTypeId == product.ProductTypeId &&
                x.Item.ProductId == product.Id);

            foreach (var cartItem in filteredCart)
            {
                var item = cartItem.Item;
                var giftCardInfoSame = true;
                var customerEnteredPricesEqual = true;
                var attributesEqual = item.AttributeSelection == selection;

                if (item.Product.IsGiftCard)
                {
                    var info1 = item.AttributeSelection?.GetGiftCardInfo();
                    var info2 = selection?.GetGiftCardInfo();

                    if (info1 != null && info2 != null)
                    {
                        // INFO: in this context, we only compare the name of recipient and sender.
                        if (!info1.RecipientName.EqualsNoCase(info2.RecipientName) || !info1.SenderName.EqualsNoCase(info2.SenderName))
                        {
                            giftCardInfoSame = false;
                        }
                    }
                }

                // Products with CustomerEntersPrice are equal if the price is the same.
                // But a system product may only be placed once in the shopping cart.
                if (customerEnteredPrice.HasValue && item.Product.CustomerEntersPrice && !item.Product.IsSystemProduct)
                {
                    customerEnteredPricesEqual = Math.Round(item.CustomerEnteredPrice, priceDigits) == Math.Round(customerEnteredPrice.Value.Amount, priceDigits);
                }

                if (attributesEqual && giftCardInfoSame && customerEnteredPricesEqual)
                {
                    return cartItem;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether the shopping cart requires shipping.
        /// </summary>
        /// <returns>
        /// <c>True</c> if any product requires shipping, otherwise <c>false</c>.
        /// </returns>
        public static bool IsShippingRequired(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.IsShippingEnabled).Any();
        }

        /// <summary>
        /// Gets the total quantity of products in the cart.
        /// </summary>
		public static int GetTotalQuantity(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Sum(x => x.Item.Quantity);
        }

        /// <summary>
        /// Gets a value indicating whether the cart includes products matching the condition.
        /// </summary>
        /// <param name="matcher">The condition to match cart items.</param>
        /// <returns><c>True</c> if any product matches the condition, otherwise <c>false</c>.</returns>
        public static bool IncludesMatchingItems(this ShoppingCart cart, Func<Product, bool> matcher)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.Product != null && matcher(x.Item.Product)).Any();
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart contains a recurring item.
        /// </summary>
        /// <returns>A value indicating whether the shopping cart contains a recurring item.</returns>
		public static bool ContainsRecurringItem(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Items.Where(x => x.Item.Product?.IsRecurring ?? false).Any();
        }

        /// <summary>
        /// Gets the recurring cycle information.
        /// </summary>
        /// <returns><see cref="RecurringCycleInfo"/>.</returns>
        public static RecurringCycleInfo GetRecurringCycleInfo(this ShoppingCart cart, ILocalizationService localizationService)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(localizationService, nameof(localizationService));

            var info = new RecurringCycleInfo();
            int? cycleLength = null;
            RecurringProductCyclePeriod? cyclePeriod = null;
            int? totalCycles = null;

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Item.Product;
                if (product == null)
                {
                    throw new InvalidOperationException($"Product with Id={cartItem.Item.ProductId} cannot be loaded.");
                }

                if (product.IsRecurring)
                {
                    if (cycleLength.HasValue && cycleLength.Value != product.RecurringCycleLength)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        cycleLength = product.RecurringCycleLength;
                    }

                    if (cyclePeriod.HasValue && cyclePeriod.Value != product.RecurringCyclePeriod)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        cyclePeriod = product.RecurringCyclePeriod;
                    }

                    if (totalCycles.HasValue && totalCycles.Value != product.RecurringTotalCycles)
                    {
                        info.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                        return info;
                    }
                    else
                    {
                        totalCycles = product.RecurringTotalCycles;
                    }
                }
            }

            if (cycleLength.HasValue && cyclePeriod.HasValue && totalCycles.HasValue)
            {
                info.CycleLength = cycleLength.Value;
                info.CyclePeriod = cyclePeriod.Value;
                info.TotalCycles = totalCycles.Value;
            }

            return info;
        }
    }
}
