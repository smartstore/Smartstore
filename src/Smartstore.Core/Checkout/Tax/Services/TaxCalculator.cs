using System;

namespace Smartstore.Core.Checkout.Tax
{
    public class TaxCalculator : ITaxCalculator
    {
        public virtual Tax CalculateTaxFromNet(decimal netPrice, decimal rate)
        {
            if (netPrice == 0 || rate == 0)
                return Tax.Zero;

            return new Tax(rate, netPrice * (rate / 100), netPrice, false);
        }

        public virtual Tax CalculateTaxFromGross(decimal grossPrice, decimal rate)
        {
            if (grossPrice == 0 || rate == 0)
                return Tax.Zero;

            return new Tax(rate, grossPrice / ((100 + rate) / 100) * (rate / 100), grossPrice, true);
        }
    }
}
