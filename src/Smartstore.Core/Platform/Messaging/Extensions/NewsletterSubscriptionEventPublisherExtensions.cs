using System.Threading.Tasks;
using Smartstore.Core.Messages.Events;
using Smartstore.Events;

namespace Smartstore.Core.Messages
{
    public static class NewsletterSubscriptionEventPublisherExtensions
    {
        /// <summary>
        /// Publishes the newsletter subscribed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="mailAddress">The mail address which has subscribed.</param>
        public static Task PublishNewsletterSubscribedAsync(this IEventPublisher eventPublisher, string mailAddress)
        {
            return mailAddress.HasValue()
                ? eventPublisher.PublishAsync(new NewsletterSubscribedEvent(mailAddress))
                : Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the newsletter unsubscribed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="mailAddress">The mail address which has unsubscribed.</param>
        public static Task PublishNewsletterUnsubscribedAsync(this IEventPublisher eventPublisher, string mailAddress)
        {
            return mailAddress.HasValue()
                ? eventPublisher.PublishAsync(new NewsletterUnsubscribedEvent(mailAddress))
                : Task.CompletedTask;
        }
    }
}
