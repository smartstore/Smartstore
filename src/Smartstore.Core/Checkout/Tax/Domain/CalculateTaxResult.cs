using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents a result of tax calculation
    /// </summary>
    public partial class CalculateTaxResult
    {
        /// <summary>
        /// Gets or sets a tax rate
        /// </summary>
        public decimal TaxRate { get; set; }

        /// <summary>
        /// Gets or sets an address
        /// </summary>
        public List<string> Errors { get; set; } = new();

        public bool Success => Errors.Count == 0;

        public void AddError(string error) => Errors.Add(error);
    }
}