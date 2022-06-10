namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Responsible for accessing the <see cref="CheckoutState"/> of a customer.
    /// </summary>
    public interface ICheckoutStateAccessor
    {
        /// <summary>
        /// Checks whether the <see cref="CheckoutState"/> instance is already loaded for the current request.
        /// </summary>
        bool IsStateLoaded { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="CheckoutState"/> instance has been changed.
        /// </summary>
        bool HasStateChanged { get; set; }

        /// <summary>
        /// The <see cref="CheckoutState"/> instance. Returns <c>null</c> if HttpContext cannot be accessed.
        /// </summary>
        CheckoutState CheckoutState { get; }

        /// <summary>
        /// Saves the current <see cref="CheckoutState"/> instance to the underlying storage if it was loaded/accessed before.
        /// </summary>
        void Save();

        /// <summary>
        /// Removes the current <see cref="CheckoutState"/> instance from the underlying storage if it is present.
        /// </summary>
        void Abandon();
    }
}
