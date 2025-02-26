using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Used to send suspend, cancel, or reactivate a subscription.
    /// </summary>
    public class SubscriptionStateChangeMessage
    {
        /// <summary>
        /// The reason for state change of a subscription. Required to reactivate the subscription.
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string Reason;
    }
}
