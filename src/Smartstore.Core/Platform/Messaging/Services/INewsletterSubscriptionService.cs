namespace Smartstore.Core.Messaging
{
    public partial interface INewsletterSubscriptionService
    {
        /// <summary>
        /// Adds or deletes a newsletter subscription and sends newsletter activation message to subscriber in case of addition.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <param name="subscribe"><c>true</c> adds subscription, <c>false</c> removes subscription</param>
        /// <returns><c>true</c> added subscription, <c>false</c> removed subscription, <c>null</c> did nothing</returns>
        Task<bool?> ApplySubscriptionAsync(bool subscribe, string email, int storeId);

        /// <summary>
        /// Activates an existing newsletter subscription and publishes corresponding event. The caller is responsible for database commit.
        /// </summary>
        /// <returns>Whether subscription was successful.</returns>
        bool Subscribe(NewsletterSubscription subscription);

        /// <summary>
        /// Deactivates an existing newsletter subscription and publishes corresponding event. The caller is responsible for database commit.
        /// </summary>
        /// <returns>Whether unsubscription was successful.</returns>
        bool Unsubscribe(NewsletterSubscription subscription);
    }
}
