using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore
{
    public static class NotifierDisplayHelperExtensions
    {
        public static bool HasMessages(this IDisplayHelper displayHelper)
        {
            return ResolveNotifications(displayHelper, null).Any();
        }

        public static ICollection<LocalizedString> GetMessages(this IDisplayHelper displayHelper, NotifyType type)
        {
            return ResolveNotifications(displayHelper, type).AsReadOnly();
        }

        public static IEnumerable<LocalizedString> ResolveNotifications(IDisplayHelper displayHelper, NotifyType? type)
        {
            var allNotifications = displayHelper.HttpContext.GetItem("AllNotifications", () =>
            {
                var result = Enumerable.Empty<NotifyEntry>();
                string key = NotifyFilterAttribute.NotificationsKey;
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

                //var viewData = new ViewDataDictionary(null); // TODO: (core) How to access current ViewDataDictionary? Was: _controllerContext.Controller.ViewData
                //if (viewData.ContainsKey(key))
                //{
                //    entries = viewData[key] as ICollection<NotifyEntry>;
                //    if (entries != null)
                //    {
                //        result = result.Concat(entries);
                //    }
                //}

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
