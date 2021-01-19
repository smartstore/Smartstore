using System.Threading.Tasks;
using Smartstore.Core.Messages;

namespace Smartstore.Core.Content.Blogs
{
    public static class BlogMessageFactoryExtensions
    {
        /// <summary>
        /// Sends a blog comment notification message to a store owner.
        /// </summary>
        public static Task<CreateMessageResult> SendBlogCommentNotificationMessage(this IMessageFactory factory, BlogComment blogComment, int languageId = 0)
        {
            Guard.NotNull(blogComment, nameof(blogComment));

            return factory.CreateMessageAsync(MessageContext.Create(MessageTemplateNames.BlogCommentStoreOwner, languageId, customer: blogComment.Customer), true, blogComment);
        }
    }
}
