using System.Collections.Generic;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents the result of a price calculation process for a single product. All monetray amounts
    /// are in the target currency and have been exchanged and converted according to input options.
    /// </summary>
    public class CalculatedPrice
    {
        public CalculatedPrice(CalculatorContext context)
        {
            Guard.NotNull(context, nameof(context));

            Product = context.Product;
            AppliedDiscounts = context.AppliedDiscounts;
            HasPriceRange = context.HasPriceRange;
            AttributePrices = context.AttributePrices;
        }

        /// <summary>
        /// The product for which a price was calculated. Not necessarily the input product,
        /// can also be a child of a grouped product, if the lowest price should be calculated.
        /// In that case this property refers to the lowest price child product.
        /// </summary>
        public Product Product { get; init; }

        /// <summary>
        /// List of discount entities that have been applied during calculation.
        /// </summary>
        public ICollection<Discount> AppliedDiscounts { get; init; }

        /// <summary>
        /// The regular price of the input <see cref="Product"/>, in the target currency, usually <see cref="Product.Price"/>
        /// </summary>
        public Money RegularPrice { get; set; }

        /// <summary>
        /// The final price of the product.
        /// </summary>
        public Money FinalPrice { get; set; }

        /// <summary>
        /// A value indicating whether the price has a range, which is mostly the case if the lowest price
        /// was determined or any tier price was applied.
        /// </summary>
        public bool HasPriceRange { get; set; }

        /// <summary>
        /// The special offer price, if any (see <see cref="Product.SpecialPrice"/>).
        /// </summary>
        public Money? OfferPrice { get; set; }

        /// <summary>
        /// The price that is initially displayed on the product detail page, if any.
        /// Includes price adjustments of preselected attributes and prices of attribute combinations.
        /// </summary>
        public Money? PreselectedPrice { get; set; }

        /// <summary>
        /// The lowest possible price of a product, if any.
        /// Includes prices of attribute combinations and tier prices. Ignores price adjustments of attributes.
        /// </summary>
        public Money? LowestPrice { get; set; }

        /// <summary>
        /// The additional charge in the target currency, if any.
        /// </summary>
        //public Money? AdditionalCharge { get; set; }

        /// <summary>
        /// Gets a list of calculated product attribute prices, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
        /// Only returned if <see cref="PriceCalculationOptions.DetermineAttributePrices"/> is activated.
        /// </summary>
        public ICollection<CalculatedAttributePrice> AttributePrices { get; init; }

        /// <summary>
        /// Tax for <see cref="FinalPrice"/>.
        /// </summary>
        public Tax? Tax { get; set; }

        /// <summary>
        /// A value indicating whether the final price includes any discounts.
        /// </summary>
        public bool HasDiscount 
        {
            get => FinalPrice < RegularPrice;
        }

        /// <summary>
        /// The saving, in percent, compared to the regular price.
        /// </summary>
        public float SavingPercent 
        { 
            get => FinalPrice < RegularPrice ?  (float)((RegularPrice - FinalPrice) / RegularPrice) * 100 : 0f;
        }

        /// <summary>
        /// The saving, as money amount, if any discount was applied.
        /// </summary>
        public Money? SavingAmount 
        {
            get => HasDiscount ? (RegularPrice - FinalPrice).WithPostFormat(null) : null;
        }
    }
}