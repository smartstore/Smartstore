using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing
{
    public class CalculatorContext : PriceCalculationContext
    {
        public CalculatorContext(PriceCalculationContext context, decimal regularPrice)
            : base(context)
        {
            RegularPrice = regularPrice;
            FinalPrice = regularPrice;
        }

        public Product AssociatedProduct { get; set; }
        public ICollection<Discount> AppliedDiscounts { get; init; }
        public ICollection<ProductVariantAttributeValue> AppliedAttributes { get; init; }
        public TierPrice AppliedTierPrice { get; set; }
        public ProductVariantAttributeCombination AppliedAttributeCombination { get; set; }

        public decimal RegularPrice { get; init; }
        public decimal? OfferPrice { get; set; }
        public decimal? SelectionPrice { get; set; }
        public decimal? LowestPrice { get; set; }
        public decimal FinalPrice { get; set; }

        public void SetFinalPrice(decimal price)
        {
            // ?????
            if (price < FinalPrice)
            {
                FinalPrice = price;
            }
        }
    }
}
