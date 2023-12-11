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

        public string ButtonShape { get; set; }

        public string ButtonColor { get; set; }

        /// <summary>
        /// Funding source to display
        /// </summary>
        public string Funding { get; set; }

        /// <summary>
        /// Info about the page where the button is rendered. Used in Ajax requests.
        /// </summary>
        public string RouteIdent { get; set; }
    }
}