using System;
using System.Collections.Generic;
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
            //AppliedTierPrice = context.AppliedTierPrice;
            //context.AppliedAttributeCombination = context.AppliedAttributeCombination;
            HasPriceRange = context.HasPriceRange;
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
        //public ICollection<ProductVariantAttributeValue> AppliedAttributes { get; init; }
        //public TierPrice AppliedTierPrice { get; set; }
        //public ProductVariantAttributeCombination AppliedAttributeCombination { get; set; }

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
        /// The price for preselected product, if any. TODO: (mg) (core) Describe.
        /// </summary>
        public Money? PreselectedPrice { get; set; }

        /// <summary>
        /// The lowest possible price, if any. TODO: (mg) (core) Describe.
        /// </summary>
        public Money? LowestPrice { get; set; }

        /// <summary>
        /// The minimum tier price, if any. TODO: (mg) (core) Describe.
        /// </summary>
        public Money? MinTierPrice { get; set; }

        // ??? (maybe delete)
        public Money? MinAttributeCombinationPrice { get; set; }

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