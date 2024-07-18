using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Smartstore.Admin.Models.Common;
using Smartstore.Core.Web;

namespace Smartstore.Admin.Components
{
    public class NewsFeedViewComponent : SmartViewComponent
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHelper _webHelper;

        public NewsFeedViewComponent(IHttpClientFactory httpClientFactory, IWebHelper webHelper)
        {
            _httpClientFactory = httpClientFactory;
            _webHelper = webHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var lang = Services.WorkContext.WorkingLanguage;
            var result = await Services.Cache.GetAsync($"admin:newsfeed{lang.UniqueSeoCode}", async ctx =>
            {
                ctx.ExpiresIn(TimeSpan.FromHours(4));

                try
                {
                    var url = "https://smartstore.com/Plugins/NewsFeed/JsonFeed";
                    var client = _httpClientFactory.CreateClient();

                    client.Timeout = TimeSpan.FromSeconds(30);

                    var formContent = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
                    {
                        new("lang", lang.UniqueSeoCode),
                        new("ip", _webHelper.GetClientIpAddress().ToString()),
                        new("modules", string.Join(',', Services.ApplicationContext.ModuleCatalog.Modules.Select(x => x.Name))),
                        new("id", Services.ApplicationContext.RuntimeInfo.ApplicationIdentifier),
                        new("auth", Services.StoreContext.CurrentStore.GetBaseUrl().TrimEnd('/'))
                    });

                    var response = await client.PostAsync(url, formContent);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new FeedModel { IsError = true, ErrorMessage = response.ReasonPhrase };
                    }

                    var channels = await response.Content.ReadFromJsonAsync<List<NewsFeedChannelModel>>();

                    // Decide to what extent to show news items in each channel
                    foreach (var channel in channels)
                    {
                        var (full, partial, minimized) = GetNewsFeedViewTypes(channel.NewsFeedItems.Count);
                        var items = channel.NewsFeedItems;
                        var i = 0;

                        foreach (var item in channel.NewsFeedItems)
                        {
                            if (i < full)
                            {
                                items[i].ViewType = "full";
                            }
                            else if (i < full + partial)
                            {
                                items[i].ViewType = "partial";
                            }
                            else if (i < full + partial + minimized)
                            {
                                items[i].ViewType = "minimized";
                            }
                            else
                            {
                                items[i].ViewType = "hidden";
                            }

                            i++;
                        }
                    }

                    return new FeedModel
                    {
                        NewsFeedCannels = channels
                    };
                }
                catch (Exception ex)
                {
                    return new FeedModel { IsError = true, ErrorMessage = ex.Message };
                }
            });

            if (!result.NewsFeedCannels.Any() && result.IsError)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage);
            }

            return View(result);
        }

        // This method takes the number of news feed items and decides how many items to show:
        // - full (picture, title, description)
        // - partially (title, description)
        // - minimized (title only)
        private static (int full, int partial, int minimized) GetNewsFeedViewTypes(int totalItems)
        {
            switch (totalItems)
            {
                case 0:
                    return (0, 0, 0);
                case 1:
                case 2:
                case 3:
                case 4:
                    // Show up to 4 items fully
                    return (totalItems, 0, 0);
                case 5:
                    // Show 3 items fully and 2 partially
                    return (3, 2, 0);
                case 6:
                    // Show 2 items fully, 3 partially and 1 minimized
                    return (2, 3, 1);
                case 7:
                    // Show 2 items fully, 3 partially and 3 minimized
                    return (2, 3, 3);
                case 8:
                    // Show 2 items fully, 2 partially and 4 minimized
                    return (2, 2, 4);
                default:
                    // Show 1 item fully, 3 partially and 5 minimized
                    return (1, 3, 5);
            }
        }
    }
}
