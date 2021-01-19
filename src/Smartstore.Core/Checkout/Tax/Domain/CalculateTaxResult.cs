using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents a result of tax calculation.
    /// </summary>
    public partial class CalculateTaxResult
    {
        /// <summary>
        /// Gets or sets a tax rate.
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether the result is successful
        /// </summary>
        public bool Success => Errors.Count == 0;
    }
}