#nullable enable

using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Messaging;

public partial interface INewsletterSubscriptionService
{
    /// <summary>
    /// Subscribes the specified email address to the newsletter.
    /// </summary>
    /// <param name="email">The email address to subscribe.</param>
    /// <param name="customer">
    /// The customer associated with the email address.
    /// If <c>null</c>, customer will be obtained via <see cref="IWorkContext.CurrentCustomer"/>.
    /// </param>
    /// <param name="active">
    /// This value indicates whether the subscription should be activated immediately. 
    /// If <c>true</c>, the subscription is active and reward points will be applied (if enabled).
    /// Otherwise, a newsletter activation message is sent to confirm the subscription.
    /// </param>
    /// <param name="storeId">
    /// The identifier of the store for which the subscription applies.
    /// If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.
    /// </param>
    /// <returns><c>true</c> if the subscription was successful. Otherwise <c>false</c>.</returns>
    Task<bool> SubscribeAsync(
        string? email,
        Customer? customer = null,
        bool active = false,
        int? storeId = null);

    /// <summary>
    /// Unsubscribes the specified email address from the newsletter subscription list.
    /// </summary>
    /// <param name="email">The email address to unsubscribe from the newsletter.</param>
    /// <param name="remove">
    /// <c>true</c> to remove the subscription record entirely and to reduce reward points (if enabled).
    /// Otherwise, a newsletter deactivation message is sent to confirm the unsubscription.
    /// </param>
    /// <param name="storeId">
    /// The identifier of the store for which the subscription applies.
    /// If <c>null</c>, store will be obtained via <see cref="IStoreContext.CurrentStore"/>.
    /// </param>
    /// <returns><c>true</c> if the unsubscription was successful. Otherwise <c>false</c>.</returns>
    Task<bool> UnsubscribeAsync(
        string? email,
        bool remove = true,
        int? storeId = null);

    /// <summary>
    /// Activates or removes the newsletter subscription associated with the specified token.
    /// </summary>
    /// <param name="token">The unique token of the newsletter subscription.</param>
    /// <param name="active">
    /// <c>true</c> to activate or <c>false</c> to remove the newsletter subscription.</param>
    /// <returns><c>true</c> if the operation was successful. Otherwise <c>false</c>.</returns>
    Task<bool> ActivateAsync(Guid token, bool active);
}