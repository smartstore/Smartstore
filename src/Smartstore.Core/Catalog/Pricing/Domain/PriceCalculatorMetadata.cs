using System;

namespace Smartstore.Core.Catalog.Pricing
{
    public sealed class PriceCalculatorMetadata
    {
        public Type CalculatorType { get; set; }
        public CalculatorTargets ValidTargets { get; set; }
        public int Order { get; set; }
    }
}
