using System;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Tax calculator interface
    /// </summary>
    public interface ITaxCalculator
    {
        /// <summary>
        /// Calculates tax for a given net price (price excluding tax).
        /// </summary>
        /// <param name="netPrice">Net price</param>
        /// <param name="rate">Tax rate</param>
        Tax CalculateTaxFromNet(decimal netPrice, decimal rate);

        /// <summary>
        /// Calculates tax for a given gross price (price including tax).
        /// </summary>
        /// <param name="grossPrice">Gross price</param>
        /// <param name="rate">Tax rate</param>
        Tax CalculateTaxFromGross(decimal grossPrice, decimal rate);
    }
}
