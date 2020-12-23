using Smartstore.Core.Customers;
using Smartstore.Core.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Checkout.Cart
{
    /// <summary>
    /// Shopping cart extension methods
    /// </summary>
    public static class ShoppingCartExtensions
    {
        /// <summary>
        /// Indicates whether the shopping cart requires shipping
        /// </summary>
        public static bool IsShippingRequired(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.IsShippingEnabled).Any();
        }

        /// <summary>
        /// Gets the total quantity of products in the cart
        /// </summary>
		public static int GetTotalQuantity(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Sum(x => x.Item.Quantity);
        }

        /// <summary>
        /// Gets a value indicating whether the shopping cart is recurring
        /// </summary>
		public static bool IsRecurring(this IEnumerable<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Where(x => x.Item.Product?.IsRecurring ?? false).Any();
        }

        /// <summary>
        /// Gets the recurring cycle information
        /// </summary>
		public static RecurringCycleInfo GetRecurringCycleInfo(this IEnumerable<OrganizedShoppingCartItem> cart, ILocalizationService localizationService)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(localizationService, nameof(localizationService));

            var cycleInfo = new RecurringCycleInfo();

            foreach (var organizedItem in cart)
            {
                var product = organizedItem.Item.Product;
                if (product is null)
                    throw new SmartException(string.Format("Product (Id={0}) cannot be loaded", organizedItem.Item.ProductId));

                if (!product.IsRecurring)
                    continue;

                if (!cycleInfo.HasValues)
                {
                    cycleInfo.CycleLength = product.RecurringCycleLength;
                    cycleInfo.CyclePeriod = product.RecurringCyclePeriod;
                    cycleInfo.TotalCycles = product.RecurringTotalCycles;
                    continue;
                }

                if (cycleInfo.CycleLength != product.RecurringCycleLength
                    || cycleInfo.CyclePeriod != product.RecurringCyclePeriod
                    || cycleInfo.CyclePeriod != product.RecurringCyclePeriod)
                {
                    cycleInfo.ErrorMessage = localizationService.GetResource("ShoppingCart.ConflictingShipmentSchedules");
                    break;
                }
            }

            return cycleInfo;
        }

        /// <summary>
        /// Get customer of shopping cart
        /// </summary>
        public static Customer GetCustomer(this IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(cart, nameof(cart));

            return cart.Count > 0 ? cart[0].Item.Customer : null;
        }
    }
}
