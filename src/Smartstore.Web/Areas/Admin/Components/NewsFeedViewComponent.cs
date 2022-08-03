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
                        new("auth", Services.StoreContext.CurrentStore.Url.TrimEnd('/'))
                    });

                    var response = await client.PostAsync(url, formContent);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new FeedModel { IsError = true, ErrorMessage = response.ReasonPhrase };
                    }

                    var channels = await response.Content.ReadFromJsonAsync<List<NewsFeedChannelModel>>();

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
    }
}
