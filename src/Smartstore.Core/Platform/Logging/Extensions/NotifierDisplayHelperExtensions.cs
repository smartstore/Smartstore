using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Localization;
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
                ICollection<NotifyEntry> entries;

                var tempData = displayHelper.Resolve<ITempDataDictionaryFactory>().GetTempData(displayHelper.HttpContext);
                if (tempData.ContainsKey(key))
                {
                    entries = tempData[key] as ICollection<NotifyEntry>;
                    if (entries != null)
                    {
                        result = result.Concat(entries);
                    }
                }

                var viewData = displayHelper.Resolve<IViewDataAccessor>().ViewData;
                if (viewData != null && viewData.ContainsKey(key))
                {
                    entries = viewData[key] as ICollection<NotifyEntry>;
                    if (entries != null)
                    {
                        result = result.Concat(entries);
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
