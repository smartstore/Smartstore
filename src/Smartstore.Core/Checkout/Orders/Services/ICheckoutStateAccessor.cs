using System;

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
        /// Checks whether the <see cref="CheckoutState"/> instance was loaded for the current request and has been changed.
        /// </summary>
        bool HasStateChanged { get; }

        /// <summary>
        /// The <see cref="CheckoutState"/> instance. Returns <c>null</c> if HttpContext cannot be accessed.
        /// </summary>
        CheckoutState CheckoutState { get; }

        /// <summary>
        /// Saves the current <see cref="CheckoutState"/> instance to the underlying storage.
        /// </summary>
        void Save();
    }
}
