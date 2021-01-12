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
        /// <param name="mail">The mail address which has subscribed.</param>
        public static Task PublishNewsletterSubscribed(this IEventPublisher eventPublisher, string mail)
        {
            if (mail.HasValue())
                return eventPublisher.PublishAsync(new NewsletterSubscribedEvent(mail));

            return Task.CompletedTask;
        }

        /// <summary>
        /// Publishes the newsletter unsubscribed event.
        /// </summary>
        /// <param name="eventPublisher">The event publisher.</param>
        /// <param name="mail">The mail address which has unsubscribed.</param>
        public static Task PublishNewsletterUnsubscribed(this IEventPublisher eventPublisher, string mail)
        {
            if (mail.HasValue())
                return eventPublisher.PublishAsync(new NewsletterUnsubscribedEvent(mail));

            return Task.CompletedTask;
        }
    }
}
