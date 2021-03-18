using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Tax
{
    /// <summary>
    /// Represents a request for a tax rate.
    /// </summary>
    public partial class TaxRateRequest
    {
        /// <summary>
        /// Gets or sets a customer
        /// </summary>
        public Customer Customer { get; init; }

        /// <summary>
        /// Gets or sets an address
        /// </summary>
        public Address Address { get; init; }

        /// <summary>
        /// Gets or sets a tax category identifier
        /// </summary>
        public int TaxCategoryId { get; init; }
    }
}