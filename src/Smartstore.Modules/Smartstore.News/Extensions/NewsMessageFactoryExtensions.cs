using System.Threading.Tasks;
using Smartstore.Core.Messaging;
using Smartstore.News.Domain;

namespace Smartstore.News.Messaging
{
    public static class NewsMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a news comment notification message to a store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendNewsCommentNotificationMessage(this IMessageFactory factory, NewsComment newsComment, int languageId = 0)
        {
            Guard.NotNull(newsComment, nameof(newsComment));
            return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.NewsCommentStoreOwner, languageId, customer: newsComment.Customer), true, newsComment);
        }
    }
}
