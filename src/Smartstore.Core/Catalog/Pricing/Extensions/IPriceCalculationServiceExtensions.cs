using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;

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
        //public static async Task<CalculatedPrice> CalculateUnitPriceAsync(
        //    this IPriceCalculationService priceCalculationService,
        //    OrganizedShoppingCartItem cartItem,
        //    bool ignoreDiscounts = false,
        //    Currency targetCurrency = null)
        //{
        //    Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
        //    Guard.NotNull(cartItem, nameof(cartItem));

        //    var options = priceCalculationService.CreateDefaultOptions(false, cartItem.Item.Customer, targetCurrency);
        //    options.IgnoreDiscounts = ignoreDiscounts;
            
        //    var context = new PriceCalculationContext(cartItem, options);

        //    return await priceCalculationService.CalculatePriceAsync(context);
        //}

        /// <summary>
        /// Calculates both the unit price and the subtotal for a given shopping cart item.
        /// The subtotal is calculated by multiplying the unit price by <see cref="ShoppingCartItem.Quantity"/>.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="cartItem">Shopping cart item.</param>
        /// <param name="ignoreDiscounts">A value indicating whether to ignore discounts.</param>
        /// <param name="targetCurrency">The target currency to use for money conversion. Obtained from <see cref="IWorkContext.WorkingCurrency"/> if <c>null</c>.</param>
        /// <returns>Calculated subtotal.</returns>
        //public static async Task<(CalculatedPrice UnitPrice, CalculatedPrice Subtotal)> CalculateSubtotalAsync(
        //    this IPriceCalculationService priceCalculationService,
        //    OrganizedShoppingCartItem cartItem,
        //    bool ignoreDiscounts = false,
        //    Currency targetCurrency = null)
        //{
        //    Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
        //    Guard.NotNull(cartItem, nameof(cartItem));

        //    var options = priceCalculationService.CreateDefaultOptions(false, cartItem.Item.Customer, targetCurrency);
        //    options.IgnoreDiscounts = ignoreDiscounts;
            
        //    var context = new PriceCalculationContext(cartItem, options);

        //    return await priceCalculationService.CalculateSubtotalAsync(context);
        //}

        /// <summary>
        /// Calculates the price adjustments of product attributes, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
        /// Typically used to display price adjustments of selected attributes on the cart page.
        /// The calculated adjustment is always a unit price.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="product">The product.</param>
        /// <param name="selection">Attribute selection. If <c>null</c> then the price adjustments of all attributes of <paramref name="product"/> are determined.</param>
        /// <param name="quantity">
        /// The product quantity. May have impact on the price, e.g. if tier prices are applied to price adjustments.
        /// Note that the calculated price is always the unit price.
        /// </param>
        /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
        /// <returns>Price adjustments of selected attributes. Key: <see cref="ProductVariantAttributeValue.Id"/>, value: attribute price adjustment.</returns>
        public static async Task<IDictionary<int, CalculatedPriceAdjustment>> CalculateAttributePriceAdjustmentsAsync(
            this IPriceCalculationService priceCalculationService,
            Product product,
            ProductVariantAttributeSelection selection = null,
            int quantity = 1,
            PriceCalculationOptions options = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));

            options ??= priceCalculationService.CreateDefaultOptions(false);

            var context = new PriceCalculationContext(product, quantity, options);
            context.Options.DeterminePriceAdjustments = true;
            context.Options.TaxFormat = null;

            context.AddSelectedAttributes(selection, product.Id);

            var price = await priceCalculationService.CalculatePriceAsync(context);
            return price.AttributePriceAdjustments.ToDictionarySafe(x => x.AttributeValue.Id);
        }

        /// <summary>
        /// Gets the base price info for a product.
        /// </summary>
        /// <param name="priceCalculationService">Price calculation service.</param>
        /// <param name="product">The product to get the base price info for.</param>
        /// <param name="options">Price calculation options. The default options are used if <c>null</c>.</param>
        /// <returns>Base price info.</returns>
        public static async Task<string> GetBasePriceInfoAsync(this IPriceCalculationService priceCalculationService, Product product, PriceCalculationOptions options = null)
        {
            Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
            Guard.NotNull(product, nameof(product));

            if (!product.BasePriceHasValue || product.BasePriceAmount == 0)
            {
                return string.Empty;
            }

            options ??= priceCalculationService.CreateDefaultOptions(false);

            var context = new PriceCalculationContext(product, options);
            var price = await priceCalculationService.CalculatePriceAsync(context);

            return priceCalculationService.GetBasePriceInfo(product, price.FinalPrice, options.TargetCurrency);
        }
    }
}
