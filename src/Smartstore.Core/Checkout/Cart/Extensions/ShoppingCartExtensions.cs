using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Checkout.Cart
{
    public static partial class ShoppingCartExtensions
    {
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
