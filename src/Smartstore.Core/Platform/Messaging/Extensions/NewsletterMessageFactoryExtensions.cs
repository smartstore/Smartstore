namespace Smartstore.Core.Messaging
{
    public static class NewsletterMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a newsletter subscription activation message.
        /// </summary>
        public static Task<CreateMessageResult> SendNewsletterSubscriptionActivationMessageAsync(this IMessageFactory factory, NewsletterSubscription subscription, int languageId = 0)
        {
            Guard.NotNull(subscription, nameof(subscription));
            return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.NewsletterSubscriptionActivation, languageId), true, subscription);
        }

        /// <summary>
        /// Sends a newsletter subscription deactivation message.
        /// </summary>
        public static Task<CreateMessageResult> SendNewsletterSubscriptionDeactivationMessageAsync(this IMessageFactory factory, NewsletterSubscription subscription, int languageId = 0)
        {
            Guard.NotNull(subscription, nameof(subscription));
            return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.NewsletterSubscriptionDeactivation, languageId), true, subscription);
        }
    }
}
