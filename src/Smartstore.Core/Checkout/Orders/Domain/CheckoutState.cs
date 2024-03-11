using Smartstore.Collections;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutState : ObservableObject
    {
        public static string CheckoutStateSessionKey => ".Smart.CheckoutState";

        /// <summary>
        /// Gets the object name of the currently selected payment method info.
        /// The object is stored in HttpContext.Session using this name.
        /// </summary>
        /// <remarks>
        /// The payment info actually belongs in this CheckoutState but for compatibility reasons we leave it as it is.
        /// </remarks>
        public static string OrderPaymentInfoName => "OrderPaymentInfo";

        /// <summary>
        /// The payment summary as displayed on the checkout confirmation page.
        /// Set after selecting the payment method, if not skipped by a payment module.
        /// </summary>
        public string PaymentSummary
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Indicates whether payment is required. <c>false</c> if the order total is 0 (nothing to pay).
        /// Set on the payment selection page if not skipped by a payment module.
        /// </summary>
        public bool IsPaymentRequired
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Indicates whether the payment method selection page was skipped.
        /// Set on the payment selection page, if not skipped by a payment module.
        /// </summary>
        public bool IsPaymentSelectionSkipped
        {
            get => GetProperty<bool>();
            set => SetProperty(value);
        }

        /// <summary>
        /// Gets a custom state object from the <see cref="CustomProperties"/> dictionary.
        /// If the object did not exist in the dictionary it will be created.
        /// The key used to save the object in the dictionary is the type short name.
        /// </summary>
        /// <typeparam name="T">The type of the custom state object to obtain or create.</typeparam>
        /// <param name="factory">
        /// An optional state object factory. If <c>null</c> an instance is created 
        /// by using the parameterless constructor of <typeparamref name="T"/>. An exception
        /// will be thrown if <typeparamref name="T"/> has no parameterless constructur.
        /// </param>
        public T GetCustomState<T>(Func<T> factory = null)
            where T : ObservableObject
        {
            var key = typeof(T).Name;

            if (!CustomProperties.TryGetValue(key, out var state) || state == null)
            {
                state = factory?.Invoke() ?? Activator.CreateInstance<T>();
                CustomProperties[key] = state;
            }

            return (T)state;
        }

        /// <summary>
        /// Tries to remove a custom state object from the <see cref="CustomProperties"/> dictionary.
        /// The key used to remove the object from the dictionary is the type short name.
        /// </summary>
        /// <typeparam name="T">The type of the custom state object to remove.</typeparam>
        /// <returns><c>true</c> if the state object existed and was removed, <c>false</c> otherwise.</returns>
        public bool RemoveCustomState<T>()
            where T : ObservableObject
        {
            return CustomProperties.Remove(typeof(T).Name);
        }

        /// <summary>
        /// Use this dictionary for any custom data required along checkout flow
        /// </summary>
        public ObservableDictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// The payment data entered on payment method selection page
        /// </summary>
        public ObservableDictionary<string, object> PaymentData { get; set; } = new();
    }
}