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
        private Product _product;
        
        public PriceCalculationContext(Product product, PriceCalculationOptions options)
            : this(product, 1, options)
        {
        }

        public PriceCalculationContext(Product product, int quantity, PriceCalculationOptions options)
        {
            Guard.NotNull(options, nameof(options));

            Product = product;
            Quantity = quantity;

            // Always work with a shallow copy of options
            Options = options.Clone();
        }

        protected PriceCalculationContext(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            Product = context.Product;
            Quantity = context.Quantity;
            Options = context.Options;
            Metadata = context.Metadata;
            AssociatedProducts = context.AssociatedProducts;
            // [...]
        }

        public Product Product
        {
            get => _product;
            set => _product = value ?? throw new ArgumentNullException(nameof(Product));
        }

        public IPriceCalculator[] Calculators { get; init; }

        public int Quantity { get; set; } = 1;

        public PriceCalculationOptions Options { get; init; }
        public Dictionary<string, object> Metadata { get; } = new();

        public ICollection<Product> AssociatedProducts { get; set; }
        public ProductBundleItemData BundleItem { get; set; }
        public decimal? AdditionalCharge { get; set; }
        public IList<object> Attributes { get; set; }
        public ICollection<ProductBundleItemData> BundleItems { get; set; }
    }
}