using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Web.Models.Checkout
{
    public abstract partial class CheckoutModelBase : ModelBase
    {
        /// <summary>
        /// Gets or sets the current checkout step name. See <see cref="CheckoutActionNames"/>.
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current checkout page is the confirm page.
        /// </summary>
        public bool IsConfirmPage
            => ActionName == CheckoutActionNames.Confirm;

        public List<string> Warnings { get; set; } = [];
        public string PreviousStepUrl { get; set; }

        public string NextStepClass
        {
            get
            {
                switch (ActionName)
                {
                    case CheckoutActionNames.BillingAddress:
                        return "select-billing-address-button";     // TODO version 6.0
                    case CheckoutActionNames.ShippingAddress:
                        return "select-shipping-address-button";    // TODO version 6.0
                    case CheckoutActionNames.ShippingMethod:
                        return "shipping-method-next-step-button";
                    case CheckoutActionNames.PaymentMethod:
                        return "payment-method-next-step-button";
                    case CheckoutActionNames.Confirm:
                        return "btn-buy";
                    default:
                        return string.Empty;
                }
            }
        }
    }
}
