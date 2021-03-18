using System;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Tax
{
    public class TaxCalculator : ITaxCalculator
    {
        public virtual Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null)
        {
            if (grossPrice == 0)
                return Tax.Zero;

            return new Tax(rate, grossPrice / ((100 + rate.Rate) / 100) * (rate.Rate / 100), 
                grossPrice, 
                true, 
                inclusive, 
                currency);
        }

        public virtual Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null)
        {
            if (netPrice == 0)
                return Tax.Zero;

            return new Tax(rate, netPrice * (rate.Rate / 100), 
                netPrice, 
                false, 
                inclusive, 
                currency);
        }
    }
}
