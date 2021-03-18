using System;

namespace Smartstore.Core.Catalog.Pricing
{
    [Flags]
    public enum CalculatorTargets
    {
        Product = 1,
        CartItem = 2,
        All = Product | CartItem
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
