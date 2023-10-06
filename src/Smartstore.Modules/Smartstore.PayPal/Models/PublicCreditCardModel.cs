namespace Smartstore.PayPal.Models
{
    public class PublicCreditCardModel : ModelBase
    {
        /// <summary>
        /// A value indicating whether the client token was retrieved from the PayPal API.
        /// </summary>
        public bool HasClientToken { get; set; }
    }
}