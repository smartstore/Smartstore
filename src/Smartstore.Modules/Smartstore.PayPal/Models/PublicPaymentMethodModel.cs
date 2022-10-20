namespace Smartstore.PayPal.Models
{
    public class PublicPaymentMethodModel : ModelBase
    {
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
        /// Funding sources to display
        /// </summary>
        public string Fundings { get; set; }

        public string ButtonShape { get; set; }

        public string ButtonColor { get; set; }

        /// <summary>
        /// Will be passed to the createOrder function of PayPal JS SDK
        /// </summary>
        public string OrderJson { get; set; }
    }
}