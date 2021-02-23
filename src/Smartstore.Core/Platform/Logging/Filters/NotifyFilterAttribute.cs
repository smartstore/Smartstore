using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Smartstore.Core.Logging
{
    public class NotifyFilterAttribute : TypeFilterAttribute
    {
        public const string NotificationsAccessKey = "sm.notifications.all";

        public NotifyFilterAttribute()
            : base(typeof(NotifyFilter))
        {
        }

        class NotifyFilter : IResultFilter
        {
            private readonly INotifier _notifier;

            public NotifyFilter(INotifier notifier)
            {
                _notifier = notifier;
            }

            public void OnResultExecuting(ResultExecutingContext context)
            {
                if (_notifier.Entries.Count == 0)
                    return;

                if (context.HttpContext.Request.IsAjaxRequest())
                {
                    HandleAjaxRequest(_notifier.Entries.FirstOrDefault(), context.HttpContext.Response);
                    return;
                }
                
                if (context.Controller is Controller controller)
                {
                    Persist(controller.ViewData, _notifier.Entries.Where(x => x.Durable == false));
                    Persist(controller.TempData, _notifier.Entries.Where(x => x.Durable == true));
                }

                _notifier.Entries.Clear();
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }

            private static void Persist(IDictionary<string, object> bag, IEnumerable<NotifyEntry> source)
            {
                if (!source.Any())
                    return;

                var existing = (bag[NotificationsAccessKey] ?? new HashSet<NotifyEntry>()) as HashSet<NotifyEntry>;

                source.Each(x =>
                {
                    if (x.Message.Value.HasValue())
                        existing.Add(x);
                });

                bag[NotificationsAccessKey] = TrimSet(existing);
            }

            private static void HandleAjaxRequest(NotifyEntry entry, HttpResponse response)
            {
                if (entry == null)
                    return;

                response.Headers.Add("X-Message-Type", entry.Type.ToString().ToLower());
                response.Headers.Add("X-Message", entry.Message.ToString());
            }

            private static HashSet<NotifyEntry> TrimSet(HashSet<NotifyEntry> entries)
            {
                if (entries.Count <= 20)
                {
                    return entries;
                }

                return new HashSet<NotifyEntry>(entries.Skip(entries.Count - 20));
            }
        }
    }
}
