using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class IPriceCalculationServiceExtensions
    {
        /// <summary>
        /// Calculates the unit price for a given shopping cart item.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="cartItem">Shopping cart item.</param>
        /// <param name="ignoreDiscounts">A value indicating whether to ignore discounts.</param>
        /// <param name="targetCurrency">The target currency to use for money conversion. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns>Calculated unit price.</returns>
        public static async Task<CalculatedPrice> CalculateUnitPriceAsync(
            this IPriceCalculationService2 priceCalculationService,
            OrganizedShoppingCartItem cartItem,
            bool ignoreDiscounts = false,
            Currency targetCurrency = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
            Guard.NotNull(cartItem, nameof(cartItem));

            var options = priceCalculationService.CreateDefaultOptions(false, cartItem.Item.Customer, targetCurrency);
            options.IgnoreDiscounts = ignoreDiscounts;
            var context = new PriceCalculationContext(cartItem, options);

            return await priceCalculationService.CalculatePriceAsync(context);
        }

        /// <summary>
        /// Calculates the subtotal (unit price multiplied by <see cref="ShoppingCartItem.Quantity"/>) for a given shopping cart item.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="cartItem">Shopping cart item.</param>
        /// <param name="ignoreDiscounts">A value indicating whether to ignore discounts.</param>
        /// <param name="targetCurrency">The target currency to use for money conversion. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns>Calculated subtotal.</returns>
        public static async Task<CalculatedPrice> CalculateSubtotalAsync(
            this IPriceCalculationService2 priceCalculationService,
            OrganizedShoppingCartItem cartItem,
            bool ignoreDiscounts = false,
            Currency targetCurrency = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
            Guard.NotNull(cartItem, nameof(cartItem));

            var options = priceCalculationService.CreateDefaultOptions(false, cartItem.Item.Customer, targetCurrency);
            options.IgnoreDiscounts = ignoreDiscounts;
            var context = new PriceCalculationContext(cartItem, options)
            {
                CalculateUnitPrice = false
            };

            return await priceCalculationService.CalculatePriceAsync(context);
        }

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="product">The product to get the base price info for.</param>
        /// <param name="customer">The customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="targetCurrency">The target currency to use for money conversion. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns></returns>
        public static async Task<string> GetBasePriceInfoAsync(
            this IPriceCalculationService2 priceCalculationService,
            Product product,
            Customer customer = null,
            Currency targetCurrency = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
            Guard.NotNull(product, nameof(product));

            if (!product.BasePriceHasValue || product.BasePriceAmount == 0)
            {
                return string.Empty;
            }

            var options = priceCalculationService.CreateDefaultOptions(false, customer, targetCurrency);
            var context = new PriceCalculationContext(product, options);
            var price = await priceCalculationService.CalculatePriceAsync(context);

            return priceCalculationService.GetBasePriceInfo(product, price.FinalPrice, options.TargetCurrency);
        }
    }
}
