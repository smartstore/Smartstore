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
        public static string GetFileManagerUrl(this IDisplayHelper displayHelper)
        {
            return displayHelper.HttpContext.GetItem("FileManagerUrl", () =>
            {
                var urlHelper = displayHelper.Resolve<IUrlHelper>();
                var defaultUrl = urlHelper.Action("Index", "RoxyFileManager", new { area = "Admin" });
                var message = new FileManagerUrlRequested
                {
                    UrlHelper = urlHelper,
                    Url = defaultUrl
                };

                displayHelper.Resolve<IEventPublisher>().Publish(message);

                return message.Url ?? defaultUrl;
            });
        }
    }
}
