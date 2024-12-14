namespace Smartstore.Web.Api.Models.Checkout
{
    public partial class PaymentRefundModel
    {
        /// <summary>
        /// A value indicating whether to refund online (refunding via payment provider) 
        /// or offline (just mark as refunded without calling the payment provider).
        /// </summary>
        public bool Online { get; set; }

        /// <summary>
        /// The amount to refund. 0 to refund the maximum refundable amount (default).
        /// </summary>
        public decimal AmountToRefund { get; set; }
    }
}
