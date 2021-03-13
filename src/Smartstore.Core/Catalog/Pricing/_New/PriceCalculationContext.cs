using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Core.Checkout.Tax;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationContext
    {
        public PriceCalculationOptions Options { get; set; }
        public ProductBatchContext BatchContext { get; set; }

        public Customer Customer { get; init; }
        public Currency Currency { get; init; }
        public Store Store { get; init; }

        public Product Product { get; init; }
        public ProductBundleItemData BundleItem { get; init; }
        public int Quantity { get; init; }
        public decimal? AdditionalCharge { get; init; }
        public IList<Product> AssociatedProducts { get; init; }
        public IList<object> Attributes { get; init; }
        public IList<object> BundleItems { get; init; }

        //public decimal InitialPrice { get; init; }
        //public decimal Price { get; set; }
        //public decimal Tax { get; set; }

        public PriceCalculationResult Result { get; set; }
    }
}