using System;

namespace Smartstore.Core.Catalog.Pricing
{
    [Flags]
    public enum CalculatorTargets
    {
        Product = 1,
        GroupedProduct = 2,
        Bundle = 4,
        All = Product | GroupedProduct | Bundle
        // More targets [...]
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class CalculatorUsageAttribute : Attribute
    {
        public CalculatorUsageAttribute(CalculatorTargets validTargets, int order)
        {
            ValidTargets = validTargets;
            Order = order;
        }

        public CalculatorTargets ValidTargets { get; }
        public int Order { get; }
    }
}
