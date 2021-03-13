using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationResult
    {
        public Product Product { get; init; }
        public bool DisplayFromMessage { get; init; }

        public IList<Discount> AppliedDiscounts { get; init; }

        public Money? RegularPrice { get; init; }
        public Money UnitPrice { get; init; }
        public Money FinalPrice { get; init; }

        public bool HasDiscount { get; set; }
        public float SavingPercent { get; set; }
        public Money? SavingAmount { get; set; }
    }
}