using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Customers;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Price calculation service.
    /// </summary>
    public partial interface IPriceCalculationService
    {
        //Task<PriceCalculationResult> Calculate(PriceCalculationRequest request, IEnumerable<IPriceCalculator> pipeline);

        /// <summary>
        /// Creates a price calculation context.
        /// </summary>
        /// <param name="products">Products. <c>null</c> to lazy load data if required.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="storeId">Store identifier. If <c>null</c>, store identifier will be obtained via <see cref="IStoreContext.CurrentStore"/>.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <returns>Price calculation context.</returns>
        PriceCalculationContext CreatePriceCalculationContext(
            IEnumerable<Product> products = null,
            Customer customer = null,
            int? storeId = null,
            bool includeHidden = true);

        /// <summary>
        /// Gets the special price, if any.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <returns>Special price or <c>null</c> if not available.</returns>
        decimal? GetSpecialPrice(Product product);

        /// <summary>
        /// Gets the product cost.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Product cost.</returns>
        Task<decimal> GetProductCostAsync(Product product, ProductVariantAttributeSelection selection);

        /// <summary>
        /// Gets the initial price including preselected attributes.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="currency">Currency.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <returns></returns>
        Task<decimal> GetPreselectedPriceAsync(Product product, Customer customer, Currency currency, PriceCalculationContext context);

        /// <summary>
        /// Gets the discount amount.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="quantity">Quantity.</param>
        /// <param name="bundleItem">Product bundle item.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <param name="finalPrice">Final product price without discount.</param>
        /// <returns>The discount amount and the applied discount.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Customer customer = null,
            decimal additionalCharge = decimal.Zero,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
            decimal? finalPrice = null);
    }
}
