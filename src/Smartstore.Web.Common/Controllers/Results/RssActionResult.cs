using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;

namespace Smartstore.Web.Controllers
{
    public class RssActionResult : ActionResult
    {
        private readonly SyndicationFeed _feed;

        public RssActionResult(SyndicationFeed feed)
        {
            _feed = Guard.NotNull(feed, nameof(feed));
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var ctx = context.HttpContext;
            var rssFormatter = new Rss20FeedFormatter(_feed, false);
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                CheckCharacters = false,
                Async = true,
                Encoding = Encoding.UTF8
            };

            ctx.Response.ContentType = "application/rss+xml";

            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, settings);

            rssFormatter.WriteTo(writer);
            await writer.FlushAsync();

            try
            {
                stream.Seek(0, SeekOrigin.Begin);

                await stream.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Don't throw this exception, it's most likely caused by the client disconnecting.
                // However, if it was cancelled for any other reason we need to prevent empty responses.
                ctx.Abort();
            }
        }
    }
}
