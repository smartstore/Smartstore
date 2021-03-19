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
        private IPricable _product;
        
        public PriceCalculationContext(IPricable product, PriceCalculationOptions options)
            : this(product, 1, options)
        {
        }

        public PriceCalculationContext(IPricable product, int quantity, PriceCalculationOptions options)
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
            IsGrossPrice = context.IsGrossPrice;
            Options = context.Options;
            Metadata = context.Metadata;
            Calculators = context.Calculators;
            AssociatedProducts = context.AssociatedProducts;
            // [...]
        }

        public IPricable Product
        {
            get => _product;
            set => _product = value ?? throw new ArgumentNullException(nameof(Store));
        }

        public int Quantity { get; set; } = 1;
        public bool? IsGrossPrice { get; set; }

        public PriceCalculationOptions Options { get; init; }
        public Dictionary<string, object> Metadata { get; } = new();

        public ICollection<Product> AssociatedProducts { get; set; }
        public ProductBundleItemData BundleItem { get; set; }
        public decimal? AdditionalCharge { get; set; }
        public IList<object> Attributes { get; set; }
        public IList<object> BundleItems { get; set; }

        public IPriceCalculator[] Calculators { get; set; }
    }
}