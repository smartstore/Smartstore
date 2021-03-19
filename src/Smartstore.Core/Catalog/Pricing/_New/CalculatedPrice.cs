using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    public class CalculatedPrice
    {
        public CalculatedPrice(CalculatorContext context)
        {
            Guard.NotNull(context, nameof(context));

            Product = context.Product;
            AppliedDiscounts = context.AppliedDiscounts;
            AppliedTierPrice = context.AppliedTierPrice;
            context.AppliedAttributeCombination = context.AppliedAttributeCombination;
            HasPriceRange = context.HasPriceRange;
        }

        public IPricable Product { get; init; }
        public IPricable ContextProduct { get; set; }
        //public bool DisplayFromMessage { get; init; }

        public ICollection<Discount> AppliedDiscounts { get; init; }
        public ICollection<ProductVariantAttributeValue> AppliedAttributes { get; init; }
        public TierPrice AppliedTierPrice { get; set; }
        public ProductVariantAttributeCombination AppliedAttributeCombination { get; set; }

        public Money RegularPrice { get; set; }
        public Money FinalPrice { get; set; }
        public bool HasPriceRange { get; set; }

        public Money? OfferPrice { get; set; }
        public Money? SelectionPrice { get; set; }
        public Money? LowestPrice { get; set; }
        public Money? MinTierPrice { get; set; }
        public Money? MinAttributeCombinationPrice { get; set; }

        /// <summary>
        /// Tax for <see cref="FinalPrice"/>.
        /// </summary>
        public Tax? Tax { get; set; }

        public bool HasDiscount 
        {
            get => FinalPrice < RegularPrice;
        }

        public float SavingPercent 
        { 
            get => FinalPrice < RegularPrice ?  (float)((RegularPrice - FinalPrice) / RegularPrice) * 100 : 0f;
        }

        public Money? SavingAmount 
        {
            get => HasDiscount ? (RegularPrice - FinalPrice).WithPostFormat(null) : null;
        }
    }
}