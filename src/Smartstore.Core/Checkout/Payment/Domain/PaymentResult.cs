using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a default payment result
    /// </summary>
    public partial class PaymentResult
    {
        private readonly List<string> _errors = new();
        /// <summary>
        /// Gets the list of errors as <see cref="IEnumerable{T}"/>.
        /// </summary>
        public IEnumerable<string> Errors => _errors;

        /// <summary>
        /// Gets a value indicating whether errors list is empty.
        /// </summary>
        public bool Success => _errors.Count == 0;

        /// <summary>
        /// Adds a new error to <see cref="_errors"/>.
        /// </summary>
        /// <param name="error">Error to add.</param>
        public void AddError(string error) => _errors.Add(error);

        /// <summary>
        /// Gets or sets a payment status after processing.
        /// </summary>
        public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;
    }
}
