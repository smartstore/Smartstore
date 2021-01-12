using System.Threading.Tasks;

namespace Smartstore.Core.Messages
{
    public partial interface INewsletterSubscriptionService
    {
        /// <summary>
        /// Adds or deletes a newsletter subscription.
        /// </summary>
        /// <param name="subscribe"><c>true</c> add subscription, <c>false</c> remove subscription</param>
        /// <returns><c>true</c> added subscription, <c>false</c> removed subscription, <c>null</c> did nothing</returns>
        Task<bool?> ApplySubscriptionAsync(bool subscribe, string email, int storeId);

        /// <summary>
        /// Activates an existing newsletter subscription and publishes corresponding event.
        /// </summary>
        /// <returns>Whether subscription was successful.</returns>
        Task<bool> SubscribeAsync(NewsletterSubscription subscription);

        /// <summary>
        /// Deactivates an existing newsletter subscription and publishes corresponding event.
        /// </summary>
        /// <returns>Whether unsubscription was successful.</returns>
        Task<bool> UnsubscribeAsync(NewsletterSubscription subscription);

        /// <summary>
        /// Updates an existing newsletter subscription and publishes corresponding events.
        /// </summary>
        /// <returns><c>true</c> subscribed now, <c>false</c> unsubscribed now, <c>null</c> nothing changed</returns>
        Task<bool?> UpdateSubscriptionAsync(NewsletterSubscription subscription);
    }
}
