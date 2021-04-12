using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class IPriceCalculationServiceExtensions
    {
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
            var finalPrice = await priceCalculationService.CalculatePriceAsync(context);

            return priceCalculationService.GetBasePriceInfo(product, finalPrice.FinalPrice, options.TargetCurrency);
        }
    }
}
