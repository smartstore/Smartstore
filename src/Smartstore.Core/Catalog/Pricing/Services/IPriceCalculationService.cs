using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Price calculation service.
    /// </summary>
    public partial interface IPriceCalculationService
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
        /// Creates and prepares a calculation context for calculating a price of a shopping cart item.
        /// Includes selected product attributes and prices of product attribute combinations.
        /// </summary>
        /// <param name="cartItem">Shopping cart item.</param>
        /// <param name="options">Price calculation options.</param>
        /// <returns>Price calculation context.</returns>
        Task<PriceCalculationContext> CreateCalculationContextAsync(OrganizedShoppingCartItem cartItem, PriceCalculationOptions options);

        /// <summary>
        /// Calculates the unit price for a given product. Prices are returned in the currency specified by <see cref="PriceCalculationOptions.TargetCurrency"/>.
        /// </summary>
        /// <param name="context">The context that contains the input product, the calculation options and some cargo data.</param>
        /// <returns>A new <see cref="CalculatedPrice"/> instance.</returns>
        Task<CalculatedPrice> CalculatePriceAsync(PriceCalculationContext context);

        /// <summary>
        /// Calculates both the unit price and the subtotal for a given product.
        /// The subtotal is calculated by multiplying the unit price (rounded if enabled for <see cref="PriceCalculationOptions.RoundingCurrency"/>)
        /// by <see cref="PriceCalculationContext.Quantity"/>.
        /// </summary>
        /// <param name="context">The context that contains the input product, the calculation options and some cargo data.</param>
        /// <returns>The unit price and the subtotal.</returns>
        Task<(CalculatedPrice UnitPrice, CalculatedPrice Subtotal)> CalculateSubtotalAsync(PriceCalculationContext context);

        /// <summary>
        /// Calculates the product cost as specified by <see cref="Product.ProductCost"/> in the primary currency.
        /// The product cost is the cost of all the different components which make up the product. This may either be the purchase price if the components are bought from outside suppliers, 
        /// or the combined cost of materials and manufacturing processes if the component is made in-house.
        /// </summary>
        /// <param name="product">Product.</param>
        /// <param name="selection">Selected attributes, if any. Used to include the costs of products linked by attributes (see <see cref="ProductVariantAttributeValue.LinkedProductId"/>).</param>
        /// <returns>Product costs in the primary currency.</returns>
        Task<Money> CalculateProductCostAsync(Product product, ProductVariantAttributeSelection selection = null);

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="price">The calculated product price.</param>
        /// <param name="targetCurrency">The currency to be used for the formatting. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <param name="language">The language to be used for the formatting. Obtained from <see cref="IWorkContext.WorkingLanguage"/> if <c>null</c>.</param>
        /// <param name="includePackageContentPerUnit">
        /// A value indicating whether to include the package content per unit information in the base price info.
        /// <c>false</c> provides a simple, language-neutral base price information, e.g. "24,90 € / 100 g".
        /// </param>
        /// <param name="displayTaxSuffix">
        /// A value indicating whether to display the tax suffix.
        /// If <c>null</c>, current setting will be obtained from <see cref="TaxSettings"/>.
        /// </param>
        /// <returns>The base price info.</returns>
        string GetBasePriceInfo(
            Product product,
            Money price,
            Currency targetCurrency = null,
            Language language = null,
            bool includePackageContentPerUnit = true,
            bool? displayTaxSuffix = null);
    }
}
