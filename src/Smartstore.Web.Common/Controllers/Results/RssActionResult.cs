using System;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Web.Controllers
{
    public class RssActionResult : ActionResult
    {
        public SyndicationFeed Feed { get; set; }

        public override void ExecuteResult(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "application/rss+xml";

            var rssFormatter = new Rss20FeedFormatter(Feed, false);
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "\t", CheckCharacters = false };

            using (var writer = XmlWriter.Create(context.HttpContext.Response.Body, settings))
            {
                rssFormatter.WriteTo(writer);
            }
        }
    }
}
