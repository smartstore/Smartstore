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

                if (context.HttpContext.Request.IsAjax() && !context.HttpContext.Request.Query.ContainsKey("silent"))
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

                var existingHolder = (bag[NotificationsAccessKey] ?? new NotifyEntriesHolder()) as NotifyEntriesHolder;

                existingHolder.Entries = existingHolder.Entries
                    .Union(source.Where(x => x.Message.HasValue()))
                    .ToArray();

                bag[NotificationsAccessKey] = TrimSet(existingHolder);
            }

            private static void HandleAjaxRequest(NotifyEntry entry, HttpResponse response)
            {
                if (entry == null)
                    return;

                response.Headers["X-Message-Type"] = entry.Type.ToString().ToLower();
                response.Headers["X-Message"] = Convert.ToBase64String(entry.Message.GetBytes());
            }

            private static NotifyEntriesHolder TrimSet(NotifyEntriesHolder holder)
            {
                if (holder.Entries.Length <= 20)
                {
                    return holder;
                }

                return new NotifyEntriesHolder { Entries = holder.Entries.Skip(holder.Entries.Length - 20).ToArray() };
            }
        }
    }
}
