using System.Threading.Tasks;
using Smartstore.Core.Identity;
using Smartstore.Core.Messaging;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums
{
    internal static partial class ForumMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a forum topic message to a customer.
        /// </summary>
        public static Task<CreateMessageResult> SendNewForumTopicMessageAsync(
            this IMessageFactory factory, 
            Customer customer,
            ForumTopic forumTopic,
            int languageId = 0)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(forumTopic, nameof(forumTopic));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.NewForumTopic, languageId, customer: customer), 
                true, 
                forumTopic, 
                forumTopic.Forum);
        }

        /// <summary>
        /// Sends a forum post message to a customer.
        /// </summary>
        /// <param name="topicPageIndex">Friendly forum topic page to use for URL generation (1-based).</param>
        public static Task<CreateMessageResult> SendNewForumPostMessageAsync(
            this IMessageFactory factory, 
            Customer customer,
            ForumPost forumPost,
            int topicPageIndex, 
            int languageId = 0)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(forumPost, nameof(forumPost));

            var bag = new ModelPart
            {
                ["TopicPageIndex"] = topicPageIndex
            };

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.NewForumPost, languageId, customer: customer), 
                true, 
                bag, 
                forumPost, 
                forumPost.ForumTopic, 
                forumPost.ForumTopic.Forum);
        }

        /// <summary>
        /// Sends a private message notification.
        /// </summary>
        public static Task<CreateMessageResult> SendPrivateMessageNotificationAsync(
            this IMessageFactory factory, 
            Customer customer, 
            PrivateMessage privateMessage, 
            int languageId = 0)
        {
            Guard.NotNull(customer, nameof(customer));
            Guard.NotNull(privateMessage, nameof(privateMessage));

            return factory.CreateMessageAsync(
                MessageContext.Create(MessageTemplateNames.NewPrivateMessage, languageId, privateMessage.StoreId, customer),
                true,
                privateMessage);
        }
    }
}
