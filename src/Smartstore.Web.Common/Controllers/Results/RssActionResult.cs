using System.IO;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Web.Controllers
{
    public class RssActionResult : ActionResult
    {
        public SyndicationFeed Feed { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "application/rss+xml";

            var rssFormatter = new Rss20FeedFormatter(Feed, false);
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "\t", CheckCharacters = false, Async = true };
            
            string content;
            using (StringWriter stringWriter = new StringWriter())
            using (var writer = XmlWriter.Create(stringWriter, settings))
            {
                rssFormatter.WriteTo(writer);
                await writer.FlushAsync();
                content = stringWriter.ToString();
            }

            await context.HttpContext.Response.WriteAsync(content);
        }
    }
}
