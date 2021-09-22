using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messaging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Seo;
using Smartstore.Events;
using Smartstore.Templating;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;

namespace Smartstore.Blog
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;

        public Events(SmartDbContext db)
        {
            _db = db;
        }

        public async Task HandleEventAsync(PreviewModelResolveEvent message, 
            ITemplateEngine engine)
        {
            if (message.ModelName == nameof(BlogComment))
            {
                message.Result = await GetRandomEntity(engine);
            }
        }

        private async Task<object> GetRandomEntity(ITemplateEngine engine)
        {
            var query = _db.BlogComments().AsNoTracking();
            var count = await query.CountAsync();

            BlogComment result;

            if (count > 0)
            {
                // Fetch a random blog comment.
                var skip = CommonHelper.GenerateRandomInteger(0, count);
                result = await query.OrderBy(x => x.Id).Skip(skip).FirstOrDefaultAsync();
            }
            else
            {
                result = new BlogComment 
                { 
                    CommentText = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, 
                                    sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua."
                };

                return engine.CreateTestModelFor(result, result.GetEntityName());
            }

            return result;
        }

        public async Task HandleEventAsync(MessageModelPartMappingEvent message, 
            IUrlHelper urlHelper, 
            IEventPublisher eventPublisher)
        {
            if (message.Source is BlogComment part)
            {
                var messageContext = message.MessageContext;
                var blogPost = await _db.BlogPosts().FindByIdAsync(part.BlogPostId);
                var url = urlHelper.RouteUrl("BlogPost", new { SeName = await blogPost.GetActiveSlugAsync(messageContext.Language.Id) });
                var title = blogPost.GetLocalized(x => x.Title, messageContext.Language).Value.NullEmpty();

                message.Result = CreateModelPart(part, messageContext, url, title);

                await eventPublisher.PublishAsync(new MessageModelPartCreatedEvent<BlogComment>(part, message.Result));
            }
        }

        // TODO: (mh) (core) I would like to throw this method away and handle everything where it is called (in HandleEventAsync).
        private static object CreateModelPart(BlogComment part, MessageContext messageContext, string url, string title)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                {  "PostTitle", title },
                {  "PostUrl", MessageModelProvider.BuildUrl(url, messageContext) },
                {  "Text", HtmlUtils.StripTags(part.CommentText).NullEmpty() }
            };

            return m;
        }

        public async Task HandleEventAsync(GdprCustomerDataExportedEvent message, 
            IMessageModelProvider messageModelProvider)
        {
            var blogComments = message.Customer.CustomerContent.OfType<BlogComment>();

            if (blogComments.Any())
            {
                message.Result["BlogComments"] = await blogComments.SelectAsync(x => messageModelProvider.CreateModelPartAsync(x, true)).AsyncToList();
            }
        }

        public void HandleEvent(CustomerAnonymizedEvent message)
        {
            var tool = message.GdprTool;

            foreach (var comment in message.Customer.CustomerContent.OfType<BlogComment>())
            {
                tool.AnonymizeData(comment, x => x.CommentText, IdentifierDataType.LongText, message.Language);
            }
        }
    }
}
