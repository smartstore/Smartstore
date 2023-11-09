using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Contains data that <see cref="IPriceCalculator"/> instances require access to.
    /// All monetary amounts are in the primary store currency, without any tax calculation applied.
    /// The calculated price is always the unit price of the product.
    /// </summary>
    public class CalculatorContext : PriceCalculationContext
    {
        public CalculatorContext(PriceCalculationContext context, decimal finalPrice, PricingType pricingType = PricingType.Calculated)
            : base(context)
        {
            PricingType = pricingType;
            FinalPrice = finalPrice;
            RegularPrice = context.Product.Price;
        }

        /// <summary>
        /// List of discount entities that have been applied during calculation.
        /// Add an entity to this collection if your calculator applied a discount to the final price.
        /// </summary>
        public ICollection<Discount> AppliedDiscounts { get; } = new HashSet<Discount>();

        /// <summary>
        /// List of discount amounts to be applied later during price calculation,
        /// e.g. the discount amount applied to a tier price.
        /// </summary>
        public ICollection<CalculatedDiscount> CalculatedDiscounts { get; } = new List<CalculatedDiscount>();

        /// <summary>
        /// Attribute combination whose price was applied during calculation.
        /// </summary>
        public ProductVariantAttributeCombination AppliedAttributeCombination { get; set; }

        /// <summary>
        /// The tier price applied during calculation of the final price.
        /// </summary>
        public TierPrice AppliedTierPrice { get; set; }

        public PricingType PricingType { get; set; }

        /// <summary>
        /// The final price of the product. A calculator should set this property if any adjustment has been made to the price.
        /// </summary>
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Pipeline emitted candidate for the regular price (unit price).
        /// The actual regular price is determined later in <see cref="PriceCalculationService.GetRegularPrice(CalculatorContext)"/>.
        /// </summary>
        public decimal RegularPrice { get; set; }

        /// <summary>
        /// A value indicating whether the price has a range, which is mostly the case if the lowest price
        /// was determined or any tier price was applied.
        /// </summary>
        public bool HasPriceRange { get; set; }

        /// <summary>
        /// The special offer price, if any (see <see cref="Product.SpecialPrice"/>).
        /// </summary>
        public decimal? OfferPrice { get; set; }

        /// <summary>
        /// The end date of the special offer price, if any (see <see cref="Product.SpecialPrice"/>).
        /// </summary>
        public DateTime? OfferEndDateUtc { get; set; }

        /// <summary>
        /// The price that is initially displayed on the product detail page, if any.
        /// Includes price adjustments of preselected attributes and prices of attribute combinations.
        /// </summary>
        public decimal? PreselectedPrice { get; set; }

        /// <summary>
        /// The lowest possible price of a product, if any.
        /// Includes prices of attribute combinations and tier prices. Ignores price adjustments of attributes.
        /// </summary>
        public decimal? LowestPrice { get; set; }

        /// <summary>
        /// Gets or sets the miniumum tier price determined during calculation.
        /// </summary>
        public decimal? MinTierPrice { get; set; }

        /// <summary>
        /// The additional charges applied to the <see cref="FinalPrice"/> during calculation, such as price adjustments of product attributes.
        /// </summary>
        /// <remarks>
        /// A calculator should add any additional charge included in <see cref="FinalPrice"/> to this property.
        /// </remarks>
        public decimal AdditionalCharge { get; set; }

        /// <summary>
        /// The discount amount resulting from applying discounts and tier prices.
        /// </summary>
        /// <remarks>
        /// A calculator should add any discount amount included in <see cref="FinalPrice"/> to this property.
        /// </remarks>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Gets or sets a list of calculated product attribute price adjustments, usually <see cref="ProductVariantAttributeValue.PriceAdjustment"/>.
        /// </summary>
        public ICollection<CalculatedPriceAdjustment> AttributePriceAdjustments { get; set; } = new List<CalculatedPriceAdjustment>();

        /// <summary>
        /// Copies all data from current context to given <paramref name="target"/> context.
        /// Mostly called in nested calculation pipelines to merge child with root data.
        /// </summary>
        public void CopyTo(CalculatorContext target)
        {
            Guard.NotNull(target);

            target.Product = Product;
            target.PricingType = PricingType;
            target.FinalPrice = FinalPrice;
            target.HasPriceRange = HasPriceRange;
            target.OfferPrice = OfferPrice;
            target.OfferEndDateUtc = OfferEndDateUtc;
            target.PreselectedPrice = PreselectedPrice;
            target.LowestPrice = LowestPrice;
            target.MinTierPrice = MinTierPrice;
            target.AdditionalCharge = AdditionalCharge;
            target.DiscountAmount = DiscountAmount;

            target.AppliedDiscounts.Clear();
            target.AppliedDiscounts.AddRange(AppliedDiscounts);

            target.CalculatedDiscounts.Clear();
            target.CalculatedDiscounts.AddRange(CalculatedDiscounts);

            target.AttributePriceAdjustments.Clear();
            target.AttributePriceAdjustments.AddRange(AttributePriceAdjustments);
        }
    }
}