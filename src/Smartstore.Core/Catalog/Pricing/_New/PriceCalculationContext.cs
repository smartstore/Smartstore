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
        public PriceCalculationContext()
        {
        }

        public PriceCalculationContext(IPricable product)
        {
            Guard.NotNull(product, nameof(product));
            Product = product;
        }

        protected PriceCalculationContext(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            Product = context.Product;
            Quantity = context.Quantity;
            Options = context.Options;
            BatchContext = context.BatchContext;
            Customer = context.Customer;
            Store = context.Store;
            Metadata = context.Metadata;
            // [...]
        }

        public IPricable Product { get; set; }
        public int Quantity { get; set; } = 1;
        public bool? IsGrossPrice { get; set; }

        public PriceCalculationOptions Options { get; set; } = new();
        public ProductBatchContext BatchContext { get; set; }

        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Dictionary<string, object> Metadata { get; } = new();
        
        public ProductBundleItemData BundleItem { get; set; }
        public decimal? AdditionalCharge { get; set; }
        public IList<Product> AssociatedProducts { get; set; }
        public IList<object> Attributes { get; set; }
        public IList<object> BundleItems { get; set; }
    }
}