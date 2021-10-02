using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Common;
using Smartstore.Web.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Smartstore.Admin.Components
{
    public class MarketplaceFeedViewComponent : SmartViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MarketplaceFeedViewComponent(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var result = await Services.Cache.GetAsync("admin:marketplacefeed", async () =>
            {
                try
                {
                    var url = "http://community.smartstore.com/index.php?/rss/downloads/";
                    var client = _httpClientFactory.CreateClient();

                    client.Timeout = TimeSpan.FromSeconds(3);

                    using var reader = XmlReader.Create(await client.GetStreamAsync(url));

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

            return View(result);
        }
    }
}
