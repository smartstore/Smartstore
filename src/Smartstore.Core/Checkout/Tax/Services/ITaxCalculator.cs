using System;
using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Tax calculator interface
    /// </summary>
    public interface ITaxCalculator
    {
        /// <summary>
        /// Calculates tax for a given gross price (price including tax).
        /// </summary>
        /// <param name="grossPrice">Gross price</param>
        /// <param name="rate">Tax rate</param>
        /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
        /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, <see cref="Result"/> will be unrounded.</param>
        Tax CalculateTaxFromGross(decimal grossPrice, TaxRate rate, bool inclusive, Currency currency = null);

        /// <summary>
        /// Calculates tax for a given net price (price excluding tax).
        /// </summary>
        /// <param name="netPrice">Net price</param>
        /// <param name="rate">Tax rate</param>
        /// <param name="inclusive">A value indicating whether the result price should be gross (inclusing tax).</param>
        /// <param name="currency">Optional currency for result price rounding. If <c>null</c>, <see cref="Result"/> will be unrounded.</param>
        Tax CalculateTaxFromNet(decimal netPrice, TaxRate rate, bool inclusive, Currency currency = null);
    }
}
