using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Events;

namespace Smartstore
{
    public class FileManagerUrlRequested
    {
        public IUrlHelper UrlHelper { get; init; }
        public string Url { get; set; }
    }

    public static class MediaDisplayHelperExtensions
    {
        public static async Task<string> GetFileManagerUrlAsync(this IDisplayHelper displayHelper)
        {
            return await displayHelper.HttpContext.GetItemAsync("FileManagerUrl", async () =>
            {
                var urlHelper = displayHelper.Resolve<IUrlHelper>();
                var defaultUrl = urlHelper.Action("Index", "RoxyFileManager", new { area = "Admin" });
                var message = new FileManagerUrlRequested
                {
                    UrlHelper = urlHelper,
                    Url = defaultUrl
                };

                await displayHelper.Resolve<IEventPublisher>().PublishAsync(message);

                return message.Url ?? defaultUrl;
            });
        }
    }
}
