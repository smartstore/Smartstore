using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Logging;
using Smartstore.Core.Web;

namespace Smartstore
{
    public static class NotifierDisplayHelperExtensions
    {
        public static bool HasMessages(this IDisplayHelper displayHelper)
        {
            return ResolveNotifications(displayHelper, null).Any();
        }

        public static ICollection<string> GetMessages(this IDisplayHelper displayHelper, NotifyType type)
        {
            return ResolveNotifications(displayHelper, type).AsReadOnly();
        }

        private static IEnumerable<string> ResolveNotifications(IDisplayHelper displayHelper, NotifyType? type)
        {
            var allNotifications = displayHelper.HttpContext.GetItem("AllNotifications", () =>
            {
                var result = Enumerable.Empty<NotifyEntry>();
                string key = NotifyFilterAttribute.NotificationsAccessKey;
                NotifyEntriesHolder holder;

                var tempData = displayHelper.Resolve<ITempDataDictionaryFactory>().GetTempData(displayHelper.HttpContext);
                if (tempData.ContainsKey(key))
                {
                    holder = tempData[key] as NotifyEntriesHolder;
                    if (holder != null)
                    {
                        result = result.Concat(holder.Entries);
                    }
                }

                var viewData = displayHelper.Resolve<IViewDataAccessor>().ViewData;
                if (viewData != null && viewData.ContainsKey(key))
                {
                    holder = viewData[key] as NotifyEntriesHolder;
                    if (holder != null)
                    {
                        result = result.Concat(holder.Entries);
                    }
                }

                return new HashSet<NotifyEntry>(result);
            });

            if (type == null)
            {
                return allNotifications.Select(x => x.Message);
            }

            return allNotifications.Where(x => x.Type == type.Value).Select(x => x.Message);
        }
    }
}
