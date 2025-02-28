namespace Smartstore.PayPal.Models
{
    public class GooglePayModel : ModelBase
    {
        /// <summary>
        /// Value that indicates whether PayPal is in sandbox mode.
        /// </summary>
        public bool IsSandbox { get; set; }

        /// <summary>
        /// Info about the page where the button is rendered. Used in Ajax requests.
        /// </summary>
        public string RouteIdent { get; set; }

        /// <summary>
        /// Value that indicates whether the PayPal button will be rendered in MiniBasket or on the payment selection page.
        /// </summary>
        public bool IsPaymentSelection { get; set; } = false;

    }
}