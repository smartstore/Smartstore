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
    public class PriceCalculationRequest
    {
        public Customer Customer { get; init; }
        public Currency Currency { get; init; }
        public Store Store { get; init; }
        public object Cart { get; init; }

        public DateTimeOffset Time { get; init; }
        public TaxDisplayType TaxDisplayType { get; init; }
        public object CalculationContext { get; init; }

        public Product Product { get; init; }
        public IList<object> Attributes { get; init; }
        public IList<object> BundleItems { get; init; }
        public int Quantity { get; init; }

        public decimal InitialPrice { get; init; }
        public decimal Price { get; set; }
        public decimal Tax { get; set; }
        public IList<object> AppliedDiscounts { get; init; }
    }
}