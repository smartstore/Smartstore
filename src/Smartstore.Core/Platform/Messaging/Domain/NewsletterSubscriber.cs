using Smartstore.Core.Identity;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Represents a newsletter subscriber with associated subscription and customer.
    /// </summary>
    public class NewsletterSubscriber
    {
        /// <summary>
        /// Newsletter subscription.
        /// </summary>
        public NewsletterSubscription Subscription { get; set; }

        /// <summary>
        /// The customer associated with the newsletter subscription. Can be <c>null</c>.
        /// </summary>
        public Customer Customer { get; set; }
    }
}
