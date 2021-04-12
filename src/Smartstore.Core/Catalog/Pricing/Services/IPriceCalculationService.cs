using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Price calculation service.
    /// </summary>
    public partial interface IPriceCalculationService2
    {
        /// <summary>
        /// Creates a new <see cref="PriceCalculationOptions"/> instance with predefined options. 
        /// The returned object is ready to be passed to <see cref="PriceCalculationContext"/> constructors.
        /// This method builds options with context defaults from <see cref="IWorkContext"/>, <see cref="CatalogSettings"/> etc.
        /// </summary>
        /// <param name="forListing">
        /// If <c>false</c>, <see cref="PriceCalculationOptions.DetermineLowestPrice" /> and <see cref="PriceCalculationOptions.DeterminePreselectedPrice"/>
        /// will also be <c>false</c>.
        /// </param>
        /// <param name="customer">The customer. Obtained from <see cref="IWorkContext.CurrentCustomer"/> if <c>null</c>.</param>
        /// <param name="targetCurrency">The target currency to use for money conversion. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <param name="batchContext">The product batch context to use during calculation. Will be created internally if <c>null</c>.</param>
        /// <returns>A new <see cref="PriceCalculationOptions"/> instance.</returns>
        PriceCalculationOptions CreateDefaultOptions(
            bool forListing,
            Customer customer = null,
            Currency targetCurrency = null,
            ProductBatchContext batchContext = null);

        /// <summary>
        /// Calculates the price for a given product.
        /// </summary>
        /// <param name="context">The context that contains the input product, the calculation options and some cargo data.</param>
        /// <returns>A new <see cref="CalculatedPrice"/> instance.</returns>
        Task<CalculatedPrice> CalculatePriceAsync(PriceCalculationContext context);

        /// <summary>
        /// Calculates the price for a shopping cart item in the primary currency.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <param name="ignoreDiscounts">A value indicating whether to ignore discounts.</param>
        /// <param name="unitPrice"><c>true</c> to calculate the unit price. <c>false</c> to calculate the subtotal price (unit price * <see cref="ShoppingCartItem.Quantity"/>).</param>
        /// <returns>A new <see cref="CalculatedPrice"/> instance.</returns>
        Task<CalculatedPrice> CalculatePriceAsync(OrganizedShoppingCartItem shoppingCartItem, bool ignoreDiscounts, bool unitPrice = true);

        /// <summary>
        /// Calculates the product cost in the primary currency.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Product cost in the primary currency.</returns>
        Task<Money> CalculateProductCostAsync(Product product, ProductVariantAttributeSelection selection);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="price">The calculated product price.</param>
        /// <param name="targetCurrency">The currency to be used for the formatting. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns>The base price info.</returns>
        string GetBasePriceInfo(Product product, Money price, Currency targetCurrency = null);
    }


    #region OLD

    /// <summary>
    /// Price calculation service.
    /// </summary>
    public partial interface IPriceCalculationService
    {
        /// <summary>
        /// Gets the special price in the primary currency. <c>null</c> if not available.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <returns>Special price in the primary currency. <c>null</c> if not available.</returns>
        Money? GetSpecialPrice(Product product);

        /// <summary>
        /// Gets the product cost in the primary currency.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Attribute selection.</param>
        /// <returns>Product cost in the primary currency.</returns>
        Task<Money> GetProductCostAsync(Product product, ProductVariantAttributeSelection selection);

        /// <summary>
        /// Gets the product price in the primary currency that is initially displayed on the product page (including preselected attributes).
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <returns>The product price in the primary currency that is initially displayed on the product page.</returns>
        Task<Money> GetPreselectedPriceAsync(Product product, Customer customer, ProductBatchContext context);

        /// <summary>
        /// Gets the lowest possible product price in the primary currency.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <returns>Lowest product price in the primary currency.</returns>
        Task<(Money LowestPrice, bool DisplayFromMessage)> GetLowestPriceAsync(Product product, Customer customer, ProductBatchContext context);

        /// <summary>
        /// Gets the lowest price in the primary currency and the related associated product of a grouped product.
        /// </summary>
        /// <param name="product">Grouped product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation service. Will be created, if <c>null</c>.</param>
        /// <param name="associatedProducts">Associated products.</param>
        /// <returns>Lowest price in the primary currency and the related associated product.</returns>
        Task<(Money? LowestPrice, Product LowestPriceProduct)> GetLowestPriceAsync(
            Product product,
            Customer customer,
            ProductBatchContext context,
            IEnumerable<Product> associatedProducts);

        /// <summary>
        /// Gets the final price in the primary currency.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <param name="bundleItem">Product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <param name="isTierPrice">A value indicating whether the price is calculated for a tier price.</param>
        /// <returns>Final product price in the primary currency.</returns>
        Task<Money> GetFinalPriceAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            bool isTierPrice = false);

        /// <summary>
        /// Gets the final price in the primary currency including bundle per-item pricing.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="bundleItems">Bundle items.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <param name="bundleItem">A product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <returns>Final product price in the primary currency.</returns>
        Task<Money> GetFinalPriceAsync(
            Product product,
            IEnumerable<ProductBundleItemData> bundleItems,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null);

        /// <summary>
        /// Gets the discount amount in the primary currency.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="additionalCharge">Additional charge value.</param>
        /// <param name="quantity">Quantity.</param>
        /// <param name="bundleItem">Product bundle item.</param>
        /// <param name="context">Price calculation context.</param>
        /// <param name="finalPrice">Final product price without discount.</param>
        /// <returns>The discount amount in the primary currency and the applied discount.</returns>
        Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            Money? finalPrice = null);

        /// <summary>
        /// Gets the discount amount in the primary currency.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <returns>The discount amount in the primary currency and the applied discount.</returns>
        Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(OrganizedShoppingCartItem shoppingCartItem);

        /// <summary>
        /// Gets the price adjustment of a variant attribute value in the primary currency.
        /// </summary>
        /// <param name="attributeValue">Product variant attribute value.</param>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer.</param>
        /// <param name="context">Price calculation context. Will be created, if <c>null</c>.</param>
        /// <param name="quantity">Product quantity.</param>
        /// <returns>Price adjustment of a variant attribute value in the primary currency.</returns>
        Task<Money> GetProductVariantAttributeValuePriceAdjustmentAsync(
            ProductVariantAttributeValue attributeValue,
            Product product,
            Customer customer,
            ProductBatchContext context,
            int quantity = 1);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="productPrice">The calculated product price.</param>
        /// <param name="currency">The currency to be used for the formatting. In general, this is always the <see cref="IWorkContext.WorkingCurrency"/>.</param>
        /// <returns>The base price info.</returns>
        string GetBasePriceInfo(Product product, Money productPrice, Currency currency);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="customer">Customer. If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.</param>
        /// <param name="currency">The currency to be used for the formatting. In general, this is always the <see cref="IWorkContext.WorkingCurrency"/>.</param>
        /// <param name="priceAdjustment">Price adjustment.</param>
        /// <returns>Base price info.</returns>
        Task<string> GetBasePriceInfoAsync(Product product, Customer customer = null, Currency currency = null, Money? priceAdjustment = null);

        /// <summary>
        /// Gets the shopping cart unit price in the primary currency.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <returns>Shopping cart unit price in the primary currency.</returns>
        Task<Money> GetUnitPriceAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts);

        /// <summary>
        /// Gets the shopping cart item sub total in the primary currency.
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item.</param>
        /// <param name="includeDiscounts">A value indicating whether to include discounts.</param>
        /// <returns>Shopping cart item sub total in the primary currency.</returns>
        Task<Money> GetSubTotalAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts);
    }

    #endregion
}
