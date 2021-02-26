using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
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
        /// Gets the lowest possible price for a product.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <returns>Lowest price.</returns>
        Task<(Money LowestPrice, bool DisplayFromMessage)> GetLowestPriceAsync(Product product, Customer customer, PriceCalculationContext context);

        /// <summary>
        /// Gets the lowest price and lowest price product of a grouped product.
        /// </summary>
        /// <param name="product">Grouped product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <param name="associatedProducts">Associated products.</param>
        /// <returns>Lowest price and lowest price product.</returns>
        Task<(Money? LowestPrice, Product LowestPriceProduct)> GetLowestPriceAsync(
            Product product,
            Customer customer,
            PriceCalculationContext context,
            IEnumerable<Product> associatedProducts);

        /// <summary>
        /// Gets the final price.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <param name="bundleItem">Product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <param name="isTierPrice">A value indicating whether the price is calculated for a tier price.</param>
        /// <returns>Final product price.</returns>
        Task<Money> GetFinalPriceAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
            bool isTierPrice = false);

        /// <summary>
        /// Gets the final price including bundle per-item pricing.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="bundleItems">Bundle items.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <param name="bundleItem">A product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <returns></returns>
        Task<Money> GetFinalPriceAsync(
            Product product,
            IEnumerable<ProductBundleItemData> bundleItems,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null);

        /// <summary>
        /// Gets the discount amount.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="quantity">Quantity.</param>
        /// <param name="bundleItem">Product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <param name="finalPrice">Final product price without discount.</param>
        /// <returns>The discount amount and the applied discount.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
            Money? finalPrice = null);

        /// <summary>
        /// Gets the discount amount.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <returns>The discount amount and the applied discount.</returns>
        Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(OrganizedShoppingCartItem shoppingCartItem);

        /// <summary>
        /// Gets the price adjustment of a variant attribute value.
        /// </summary>
        /// <param name="attributeValue">Product variant attribute value.</param>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation context. Will be created, if <c>null</c>.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <returns>Price adjustment of a variant attribute value.</returns>
        Task<decimal> GetProductVariantAttributeValuePriceAdjustmentAsync(
            ProductVariantAttributeValue attributeValue,
            Product product,
            Customer customer,
            PriceCalculationContext context,
            int quantity = 1);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="productPrice">The calculated product price.</param>
        /// <param name="currency">Target currency.</param>
        /// <returns>The base price info.</returns>
        string GetBasePriceInfo(Product product, decimal productPrice, Currency currency);

        /// <summary>
        /// /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="customer">Currency. If <c>null</c>, currency will be obtained via <see cref="IWorkContext.WorkingCurrency"/>.</param>
        /// <param name="priceAdjustment">Price adjustment.</param>
        /// <returns>Base price info.</returns>
        Task<string> GetBasePriceInfoAsync(Product product, Customer customer = null, Currency currency = null, decimal priceAdjustment = decimal.Zero);

        /// <summary>
        /// Gets the shopping cart unit price.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <returns>Shopping cart unit price.</returns>
        Task<decimal> GetUnitPriceAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts);

        /// <summary>
        /// Gets the shopping cart item sub total.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <returns>Shopping cart item sub total.</returns>
        Task<decimal> GetSubTotalAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts);
    }
}
