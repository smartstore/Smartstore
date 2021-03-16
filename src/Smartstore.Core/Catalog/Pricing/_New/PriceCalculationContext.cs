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

        public IPricable Product { get; init; }
        public int Quantity { get; init; } = 1;

        public PriceCalculationOptions Options { get; set; }
        public ProductBatchContext BatchContext { get; set; }

        public Customer Customer { get; init; }
        public Store Store { get; init; }
        public Dictionary<string, object> Metadata { get; } = new();
        
        public ProductBundleItemData BundleItem { get; init; }
        public decimal? AdditionalCharge { get; init; }
        public IList<Product> AssociatedProducts { get; init; }
        public IList<object> Attributes { get; init; }
        public IList<object> BundleItems { get; init; }
    }
}