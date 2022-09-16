namespace Smartstore.PayPal.Models
{
    public class PublicPaymentMethodModel : ModelBase
    {
        /// <summary>
        /// Specifies whether the payment will be captured immediately or just authorized.
        /// </summary>
        public string Intent { get; set; }

        /// <summary>
        /// The subtotal with discount. Will be used to create the PayPal order. 
        /// The final amount will not be passed here but in an API-Call with an OrdersPatchRequest.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Value that indicates whether the PayPal button will be rendered in MiniBasket or on the pasyment selection page.
        /// </summary>
        public bool IsPaymentSelection { get; set; } = false;

        /// <summary>
        /// Defines whether PayPal is the first payment method to be displayed on payment selection page.
        /// In this case we must handle visibility differently.
        /// </summary>
        public bool IsSelectedMethod { get; set; }

        /// <summary>
        /// Url for PayPal JavaScript URL including all needed parameters e.g. currency, client-id, etc
        /// </summary>
        public string ScriptUrl { get; set; }

        public string ButtonShape { get; set; }

        public string ButtonColor { get; set; }
    }
}