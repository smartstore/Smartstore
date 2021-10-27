using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.News.Domain;
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

namespace Smartstore.News
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
            if (message.ModelName == nameof(NewsComment))
            {
                message.Result = await GetRandomEntity(engine);
            }
        }

        private async Task<object> GetRandomEntity(ITemplateEngine engine)
        {
            var query = _db.NewsComments().AsNoTracking();
            var count = await query.CountAsync();

            NewsComment result;

            if (count > 0)
            {
                // Fetch a random news comment.
                var skip = CommonHelper.GenerateRandomInteger(0, count);
                result = await query.OrderBy(x => x.Id).Skip(skip).FirstOrDefaultAsync();
            }
            else
            {
                result = new NewsComment 
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
            MessageModelHelper messageModelHelper)
        {
            if (message.Source is NewsComment part)
            {
                var messageContext = message.MessageContext;
                var newsItem = await _db.NewsItems().FindByIdAsync(part.NewsItemId);
                var url = urlHelper.RouteUrl("NewsItem", new { SeName = await newsItem.GetActiveSlugAsync(messageContext.Language.Id) });
                var title = newsItem.GetLocalized(x => x.Title, messageContext.Language).Value.NullEmpty();

                message.Result = CreateModelPart(part, messageContext, messageModelHelper, url, title);

                await messageModelHelper.PublishModelPartCreatedEventAsync(part, message.Result);
            }
        }

        private static object CreateModelPart(NewsComment part, MessageContext messageContext, MessageModelHelper helper, string url, string title)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                {  "NewsTitle", title },
                {  "NewsUrl", helper.BuildUrl(url, messageContext) },
                {  "Title", part.CommentTitle.RemoveHtml().NullEmpty() },
                {  "Text", HtmlUtility.SanitizeHtml(part.CommentText, HtmlSanitizerOptions.UserCommentSuitable).NullEmpty() }
            };

            return m;
        }

        public async Task HandleEventAsync(GdprCustomerDataExportedEvent message, 
            IMessageModelProvider messageModelProvider)
        {
            var newsComments = message.Customer.CustomerContent.OfType<NewsComment>();

            if (newsComments.Any())
            {
                message.Result["NewsComments"] = await newsComments.SelectAsync(x => messageModelProvider.CreateModelPartAsync(x, true)).AsyncToList();
            }
        }

        public void HandleEvent(CustomerAnonymizedEvent message)
        {
            var tool = message.GdprTool;

            foreach (var comment in message.Customer.CustomerContent.OfType<NewsComment>())
            {
                tool.AnonymizeData(comment, x => x.CommentText, IdentifierDataType.LongText, message.Language);
                tool.AnonymizeData(comment, x => x.CommentTitle, IdentifierDataType.LongText, message.Language);
            }
        }
    }
}
