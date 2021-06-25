using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Common;
using Smartstore.Core;
using Smartstore.Web.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Smartstore.Admin.Components
{
    public class MarketplaceFeedViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var watch = new Stopwatch();
            watch.Start();

            var result = await Services.Cache.GetAsync("admin:marketplacefeed", async () =>
            {
                try
                {
                    string url = "http://community.smartstore.com/index.php?/rss/downloads/";
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 3000;
                    request.UserAgent = $"Smartstore {SmartstoreVersion.CurrentFullVersion}";

                    using WebResponse response = await request.GetResponseAsync();
                    using var reader = XmlReader.Create(response.GetResponseStream());
                    var feed = SyndicationFeed.Load(reader);
                    var model = new List<FeedItemModel>();
                    foreach (var item in feed.Items)
                    {
                        if (!item.Id.EndsWith("error=1", StringComparison.OrdinalIgnoreCase))
                        {
                            var modelItem = new FeedItemModel
                            {
                                Title = item.Title.Text,
                                Summary = item.Summary.Text.RemoveHtml().Truncate(150, "..."),
                                PublishDate = item.PublishDate.LocalDateTime.Humanize()
                            };

                            var link = item.Links.FirstOrDefault();
                            if (link != null)
                            {
                                modelItem.Link = link.Uri.ToString();
                            }

                            model.Add(modelItem);
                        }
                    }

                    return model;
                }
                catch (Exception ex)
                {
                    return  new List<FeedItemModel> { new FeedItemModel { IsError = true, Summary = ex.Message } };
                }
            });
            
            if (result.Any() && result.First().IsError)
            {
                ModelState.AddModelError(string.Empty, result.First().Summary);
            }

            watch.Stop();
            Debug.WriteLine("MarketplaceFeed >>> " + watch.ElapsedMilliseconds);

            return View(result);
        }
    }
}
